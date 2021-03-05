using Sara.Lib.Data.Parsers;
using Sara.Lib.Parser;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;

namespace Sara.Lib.Data.MSSQL
{
    public class MSSQLFilterParser : FilterParser
    {
        protected override Visitor GetVisitor(ParamsType paramsType, IEnumerable<string> columns, dynamic initialState = null)
        {
            var allowedOperators = AllowedOperators(paramsType);

            dynamic state = initialState;
            if (initialState == null)
            {
                state = new ExpandoObject();
                state.Parameters = new ExpandoObject();
                state.ParametersAsDict = state.Parameters as IDictionary<string, object>;
                state.Predicates = new Stack<string>();
                state.PredicateDict = new Dictionary<int, string>();       // for parameters.
                state.Sql = string.Empty;
            }

            var visitor = new Visitor(state);
            visitor.State.Columns = columns.ToList();

            visitor.AddVisitor(
                "search_condition",
                (v, n) =>
                {
                    dynamic searchCondition = n.Properties["OR"];
                    foreach (var item in (IEnumerable<Object>)searchCondition)
                    {
                        var node = item as Node;
                        if (node == null)
                            throw new Exception("Array element type not Node.");
                        node.Accept(v);
                    }

                    if (paramsType == ParamsType.FILTER)
                    {
                        List<string> items = new List<string>();
                        foreach (var item in (IEnumerable<Object>)n.Properties["OR"])
                        {
                            items.Add(v.State.Predicates.Pop());
                        }

                        var sql = string.Format("{0}", string.Join(" OR ", items.ToArray()));
                        v.State.Predicates.Push(sql);
                        v.State.Sql = $"WHERE {sql}";
                    }
                    else
                    {
                        Dictionary<int, string> dict = v.State.PredicateDict;
                        var values = dict.OrderBy(d => d.Key).Select(d => d.Value).ToList();
                        v.State.Sql = string.Join(", ", values);
                    }
                }
            );

            visitor.AddVisitor(
            "boolean_term",
            (v, n) =>
            {
                foreach (var item in (IEnumerable<Object>)n.Properties["AND"])
                {
                    var node = item as Node;
                    if (node == null)
                        throw new Exception("Array element type not Node.");
                    node.Accept(v);
                }

                // Pop the individual conditions, and create new expression which we push back on.
                if (paramsType == ParamsType.FILTER)
                {
                    List<string> items = new List<string>();
                    foreach (var item in (IEnumerable<Object>)n.Properties["AND"])
                    {
                        items.Add(v.State.Predicates.Pop());
                    }

                    var sql = string.Format("{0} ", string.Join(" AND ", items.ToArray()));
                    v.State.Predicates.Push(sql);
                }
            }
            );

            visitor.AddVisitor(
                "boolean_primary",
                (v, n) =>
                {
                    // If CONDITION property present, then need to wrap () around condition.
                    if (n.Properties.ContainsKey("CONDITION"))
                    {
                        var node = n.Properties["CONDITION"] as Node;
                        if (node == null)
                            throw new Exception("Array element type not Node.");

                        node.Accept(v);

                        var predicates = ((Stack<string>)v.State.Predicates).Pop();
                        var sql = string.Format("({0})", predicates);
                        v.State.Predicates.Push(sql);
                    }
                }
            );

            visitor.AddVisitor(
            "comparison_predicate",
            (v, n) =>
            {
                var operators = allowedOperators;
                var operatorTokenName = (string)((Token)n.Properties["OPERATOR"]).TokenName;
                if (!operators.ContainsKey(operatorTokenName))
                    throw new Exception(string.Format("Operator '{0}' not supported in this scenario.", operatorTokenName));

                var i = ((IDictionary<string, object>)v.State.ParametersAsDict).Keys.Count;
                var sql = "";
                if (paramsType == ParamsType.FILTER)
                {
                    sql = string.Format(
                        "{0} {1} @{2}",
                        ((Token)n.Properties["LHV"]).TokenValue,
                        operators[operatorTokenName],
                        "P" + i
                    );
                }
                else if (paramsType == ParamsType.PARAMS)
                {
                    sql = string.Format(
                        "@{0}",
                        "P" + i
                    );
                }
                // Check that the column identifier exists.
                if (!((IEnumerable<string>)v.State.Columns).Contains(((Token)n.Properties["LHV"]).TokenValue, StringComparer.OrdinalIgnoreCase))
                {
                    throw new Exception(string.Format("Column '{0}' does not exist!", ((Token)n.Properties["LHV"]).TokenValue));
                }

                // Add the SQL + update args object.
                if (paramsType == ParamsType.FILTER)
                    v.State.Predicates.Push(sql);
                else if (paramsType == ParamsType.PARAMS)
                {
                    int key = v.State.Columns.IndexOf(((Token)n.Properties["LHV"]).TokenValue);
                    v.State.PredicateDict[key] = sql;
                }

                object value = ((Token)n.Properties["RHV"]).TokenValue.Replace("'", "");
                ((IDictionary<string, object>)v.State.ParametersAsDict).Add("P" + i, value);
            }
        );

            visitor.AddVisitor(
                "in_predicate",
                (v, n) =>
                {
                    var i = ((IDictionary<string, object>)v.State.ParametersAsDict).Keys.Count;
                    var sql = "";
                    if (paramsType == ParamsType.FILTER)
                    {
                        sql = string.Format(
                            "{0} {1} @{2}",
                            ((Token)n.Properties["LHV"]).TokenValue,
                            n.Properties.ContainsKey("NOT") ? "NOT IN" : "IN",
                            "P" + i
                        );
                    }
                    else if (paramsType == ParamsType.PARAMS)
                        throw new Exception("IN operator not supported for parameters.");

                    // Check that the column identifier exists.
                    if (!((IEnumerable<string>)v.State.Columns).Contains(((Token)n.Properties["LHV"]).TokenValue, StringComparer.OrdinalIgnoreCase))
                    {
                        throw new Exception(string.Format("Column '{0}' does not exist!", ((Token)n.Properties["LHV"]).TokenValue.Replace("'", "")));
                    }

                    // Add the SQL + update args object.
                    v.State.Predicates.Push(sql);
                    object value = ((List<object>)n.Properties["RHV"]).Select(t => ((Token)t).TokenValue.Replace("'", ""));
                    ((IDictionary<string, object>)v.State.ParametersAsDict).Add("P" + i, value);
                }
            );

            visitor.AddVisitor(
                "between_predicate",
                (v, n) =>
                {
                    var i = ((IDictionary<string, object>)v.State.ParametersAsDict).Keys.Count;
                    var sql = "";
                    if (paramsType == ParamsType.FILTER)
                    {
                        sql = string.Format(
                            "{0} {1} @{2} AND @{3}",
                            ((Token)n.Properties["LHV"]).TokenValue,
                            n.Properties.ContainsKey("NOT") ? "NOT BETWEEN" : "BETWEEN",
                            "P" + i,
                            "P" + (i + 1)
                        );
                    }
                    else
                    {
                        throw new Exception("BETWEEN operator not supported for parameters.");
                    }

                    // Check that the column identifier exists.
                    if (!((IEnumerable<string>)v.State.Columns).Contains(((Token)n.Properties["LHV"]).TokenValue, StringComparer.OrdinalIgnoreCase))
                    {
                        throw new Exception(string.Format("Column '{0}' does not exist!", ((Token)n.Properties["LHV"]).TokenValue.Replace("'", "")));
                    }

                    v.State.Predicates.Push(sql);
                    object value1 = ((Token)n.Properties["OP1"]).TokenValue.Replace("'", "");
                    object value2 = ((Token)n.Properties["OP2"]).TokenValue.Replace("'", "");
                    ((IDictionary<string, object>)v.State.ParametersAsDict).Add("P" + i, value1);
                    ((IDictionary<string, object>)v.State.ParametersAsDict).Add("P" + (i + 1), value2);
                }
            );

            visitor.AddVisitor(
                "contains_predicate",
                (v, n) =>
                {
                    var i = ((IDictionary<string, object>)v.State.ParametersAsDict).Keys.Count;
                    var sql = "";
                    if (paramsType == ParamsType.FILTER)
                    {
                        sql = string.Format(
                        "{0} {1} @{2}",
                        ((Token)n.Properties["LHV"]).TokenValue,
                        n.Properties.ContainsKey("NOT") ? "NOT LIKE" : "LIKE",
                        "P" + i
                    );
                    }
                    else
                    {
                        throw new Exception("CONTAINS operator not supported for parameters.");
                    }

                    // Check that the column identifier exists.
                    if (!((IEnumerable<string>)v.State.Columns).Contains(((Token)n.Properties["LHV"]).TokenValue, StringComparer.OrdinalIgnoreCase))
                    {
                        throw new Exception(string.Format("Column '{0}' does not exist!", ((Token)n.Properties["LHV"]).TokenValue.Replace("'", "")));
                    }

                    v.State.Predicates.Push(sql);
                    object value = ((Token)n.Properties["RHV"]).TokenValue.Replace("'", "");
                    value = $"%{value}%";   // Add wildcards either side.
                    ((IDictionary<string, object>)v.State.ParametersAsDict).Add("P" + i, value);
                }
            );

            visitor.AddVisitor(
                "blank_predicate",
                (v, n) =>
                {
                    var i = ((IDictionary<string, object>)v.State.ParametersAsDict).Keys.Count;
                    var sql = "";
                    if (paramsType == ParamsType.FILTER)
                    {
                        sql = string.Format(
                            "{0} {1}",
                            ((Token)n.Properties["LHV"]).TokenValue,
                            n.Properties.ContainsKey("NOT") ? "IS NOT NULL" : "IS NULL"
                        );
                    }
                    else
                    {
                        throw new Exception("ISBLANK operator not supported for parameters.");
                    }

                    // Check that the column identifier exists.
                    if (!((IEnumerable<string>)v.State.Columns).Contains(((Token)n.Properties["LHV"]).TokenValue, StringComparer.OrdinalIgnoreCase))
                    {
                        throw new Exception(string.Format("Column '{0}' does not exist!", ((Token)n.Properties["LHV"]).TokenValue.Replace("'", "")));
                    }

                    v.State.Predicates.Push(sql);
                }
            );

            return visitor;
        }
    }
}
