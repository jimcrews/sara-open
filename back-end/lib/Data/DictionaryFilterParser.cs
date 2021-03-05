using Microsoft.CodeAnalysis.CSharp.Syntax;
using Sara.Lib.Data.Parsers;
using Sara.Lib.Parser;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;
using Sara.Lib.Extensions;

namespace Sara.Lib.Data.Mock
{
    /// <summary>
    /// A filter parser over a collection of dictionary objects. This is the default
    /// implementation of the FilterParser base class. This implementation can be used
    /// by any custom data lake provider. Note that this implementation filters by
    /// inspecting every item in an collection so is not recommended for large datasets.
    /// </summary>
    public class DictionaryFilterParser : FilterParser
    {
        protected override Visitor GetVisitor(ParamsType paramsType, IEnumerable<string> columns, dynamic initialState = null)
        {
            var allowedOperators = AllowedOperators(paramsType);

            dynamic state = initialState;
            if (initialState == null)
            {
                state = new ExpandoObject();

                // Filters are added to a stack
                state.FilterFunctions = new Stack<Func<IDictionary<string, object>, bool>>();
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
                        List<Func<IDictionary<string, object>, bool>> items = new List<Func<IDictionary<string, object>, bool>>();
                        foreach (var item in (IEnumerable<Object>)n.Properties["OR"])
                        {
                            items.Add(v.State.FilterFunctions.Pop());
                        }

                        Func<IDictionary<string, object>, bool> filter = (row) =>
                        {
                            bool match = false;
                            foreach (var item in items)
                            {
                                if (item(row))
                                {
                                    match = true;
                                    break;
                                }
                            }
                            return match;
                        };

                        v.State.FilterFunctions.Push(filter);
                    }
                    else
                    {
                        // Params not supported.
                        throw new Exception("Params not supported.");
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
                    List<Func<IDictionary<string, object>, bool>> items = new List<Func<IDictionary<string, object>, bool>>();
                    foreach (var item in (IEnumerable<Object>)n.Properties["AND"])
                    {
                        items.Add(v.State.FilterFunctions.Pop());
                    }

                    Func<IDictionary<string, object>, bool> filter = (row) =>
                    {
                        bool match = true;
                        foreach (var item in items)
                        {
                            if (!item(row))
                            {
                                match = false;
                                break;
                            }
                        }
                        return match;
                    };

                    v.State.FilterFunctions.Push(filter);
                }
            }
            );

            visitor.AddVisitor(
                "boolean_primary",
                (v, n) =>
                {
                    // This checks for version of boolean_primary which has a
                    // CONDITION property inside it. This is when user has used
                    // parentheses for nested subquery.
                    if (n.Properties.ContainsKey("CONDITION"))
                    {
                        var node = n.Properties["CONDITION"] as Node;
                        if (node == null)
                            throw new Exception("Array element type not Node.");

                        // inner filter will get pushed onto stack
                        node.Accept(v);

                        var innerFilter = ((Stack<Func<IDictionary<string, object>, bool>>)v.State.FilterFunctions).Pop();
                        v.State.FilterFunctions.Push(innerFilter);
                    }
                }
            );

            visitor.AddVisitor(
                "comparison_predicate",
                (v, n) =>
                {
                    string columnName = "";
                    var operators = allowedOperators;
                    var operatorTokenName = (string)((Token)n.Properties["OPERATOR"]).TokenName;
                    object value = ((Token)n.Properties["RHV"]).TokenValue.Replace("'", "");
                    if (!operators.ContainsKey(operatorTokenName))
                        throw new Exception(string.Format("Operator '{0}' not supported in this scenario.", operatorTokenName));

                    // Check that the column identifier exists.
                    if (!((IEnumerable<string>)v.State.Columns).Contains(((Token)n.Properties["LHV"]).TokenValue, StringComparer.OrdinalIgnoreCase))
                    {
                        throw new Exception(string.Format("Column '{0}' does not exist!", ((Token)n.Properties["LHV"]).TokenValue.Replace("'", "")));
                    }
                    else
                    {
                        columnName = ((IEnumerable<string>)v.State.Columns).First(c => c.Equals(((Token)n.Properties["LHV"]).TokenValue, StringComparison.OrdinalIgnoreCase));
                    }

                    // If got here, add filter predicate
                    Func<IDictionary<string, object>, bool> filter = (row) =>
                    {
                        bool match = false;
                        switch (operatorTokenName)
                        {
                            case "EQ_OP":
                                match = row[columnName].CompareTo(value) == 0;
                                break;
                            case "NE_OP":
                                match = row[columnName].CompareTo(value) != 0;
                                break;
                            case "LT_OP":
                                match = row[columnName].CompareTo(value) < 0;
                                break;
                            case "LE_OP":
                                match = row[columnName].CompareTo(value) <= 0;
                                break;
                            case "GT_OP":
                                match = row[columnName].CompareTo(value) > 0;
                                break;
                            case "GE_OP":
                                match = row[columnName].CompareTo(value) >= 0;
                                break;
                            default:
                                throw new Exception("Invalid operator");
                        }
                        return match;
                    };

                    v.State.FilterFunctions.Push(filter);
                }
            );

            visitor.AddVisitor(
                "in_predicate",
                (v, n) =>
                {
                    string columnName = "";
                    bool not = n.Properties.ContainsKey("NOT");
                    List<string> value = ((List<object>)n.Properties["RHV"]).Select(t => ((Token)t).TokenValue.Replace("'", "")).ToList();

                    if (paramsType != ParamsType.FILTER)
                    {
                        throw new Exception("CONTAINS operator not supported for parameters.");
                    }

                    // Check that the column identifier exists.
                    if (!((IEnumerable<string>)v.State.Columns).Contains(((Token)n.Properties["LHV"]).TokenValue, StringComparer.OrdinalIgnoreCase))
                    {
                        throw new Exception(string.Format("Column '{0}' does not exist!", ((Token)n.Properties["LHV"]).TokenValue.Replace("'", "")));
                    }
                    else
                    {
                        columnName = ((IEnumerable<string>)v.State.Columns).First(c => c.Equals(((Token)n.Properties["LHV"]).TokenValue, StringComparison.OrdinalIgnoreCase));
                    }

                    // If got here, add filter predicate
                    Func<IDictionary<string, object>, bool> filter = (row) =>
                    {
                        bool match = false;
                        foreach (var item in value)
                        {
                            if (row[columnName].CompareTo(item) == 0)
                            {
                                match = true;
                                break;
                            }
                        }
                        return match;
                    };

                    Func<IDictionary<string, object>, bool> notFilter = (row) =>
                    {
                        bool match = false;
                        foreach (var item in value)
                        {
                            if (row[columnName].CompareTo(item) == 0)
                            {
                                match = true;
                                break;
                            }
                        }
                        return !match;
                    };

                    if (not)
                        v.State.FilterFunctions.Push(notFilter);
                    else
                        v.State.FilterFunctions.Push(filter);
                }
            );

            visitor.AddVisitor(
                "between_predicate",
                (v, n) =>
                {
                    string columnName = "";
                    bool not = n.Properties.ContainsKey("NOT");
                    object value1 = ((Token)n.Properties["OP1"]).TokenValue.Replace("'", "");
                    object value2 = ((Token)n.Properties["OP2"]).TokenValue.Replace("'", "");

                    if (paramsType != ParamsType.FILTER)
                    {
                        throw new Exception("BETWEEN operator not supported for parameters.");
                    }

                    // Check that the column identifier exists.
                    if (!((IEnumerable<string>)v.State.Columns).Contains(((Token)n.Properties["LHV"]).TokenValue, StringComparer.OrdinalIgnoreCase))
                    {
                        throw new Exception(string.Format("Column '{0}' does not exist!", ((Token)n.Properties["LHV"]).TokenValue.Replace("'", "")));
                    }
                    else
                    {
                        columnName = ((IEnumerable<string>)v.State.Columns).First(c => c.Equals(((Token)n.Properties["LHV"]).TokenValue, StringComparison.OrdinalIgnoreCase));
                    }

                    // If got here, add filter predicate
                    Func<IDictionary<string, object>, bool> filter = (row) => { return row[columnName].CompareTo(value1) >= 0 && row[columnName].CompareTo(value2) <= 0 ; };
                    Func<IDictionary<string, object>, bool> notFilter = (row) => { return !(row[columnName].CompareTo(value1) >= 0 && row[columnName].CompareTo(value2) <= 0); };

                    if (not)
                        v.State.FilterFunctions.Push(notFilter);
                    else
                        v.State.FilterFunctions.Push(filter);
                }
            );

            visitor.AddVisitor(
                "contains_predicate",
                (v, n) =>
                {
                    string columnName = "";
                    bool not = n.Properties.ContainsKey("NOT");
                    string value = ((Token)n.Properties["RHV"]).TokenValue.Replace("'", "");

                    if (paramsType != ParamsType.FILTER)
                    {
                        throw new Exception("CONTAINS operator not supported for parameters.");
                    }

                    // Check that the column identifier exists.
                    if (!((IEnumerable<string>)v.State.Columns).Contains(((Token)n.Properties["LHV"]).TokenValue, StringComparer.OrdinalIgnoreCase))
                    {
                        throw new Exception(string.Format("Column '{0}' does not exist!", ((Token)n.Properties["LHV"]).TokenValue.Replace("'", "")));
                    }
                    else
                    {
                        columnName = ((IEnumerable<string>)v.State.Columns).First(c => c.Equals(((Token)n.Properties["LHV"]).TokenValue, StringComparison.OrdinalIgnoreCase));
                    }

                    // If got here, add filter predicate
                    Func<IDictionary<string, object>, bool> filter = (row) => { return row[columnName].ToString().Contains(value); };
                    Func<IDictionary<string, object>, bool> notFilter = (row) => { return !row[columnName].ToString().Contains(value); };

                    if (not)
                        v.State.FilterFunctions.Push(notFilter);
                    else
                        v.State.FilterFunctions.Push(filter);
                }
            );

            visitor.AddVisitor(
                "blank_predicate",
                (v, n) =>
                {
                    string columnName = "";
                    bool not = n.Properties.ContainsKey("NOT");

                    if (paramsType != ParamsType.FILTER)
                    {
                        throw new Exception("ISBLANK operator not supported for parameters.");
                    }

                    // Check that the column identifier exists.
                    if (!((IEnumerable<string>)v.State.Columns).Contains(((Token)n.Properties["LHV"]).TokenValue, StringComparer.OrdinalIgnoreCase))
                    {
                        throw new Exception(string.Format("Column '{0}' does not exist!", ((Token)n.Properties["LHV"]).TokenValue.Replace("'", "")));
                    } else
                    {
                        columnName = ((IEnumerable<string>)v.State.Columns).First(c => c.Equals(((Token)n.Properties["LHV"]).TokenValue, StringComparison.OrdinalIgnoreCase));
                    }

                    // If got here, add filter predicate
                    Func<IDictionary<string, object>, bool> filter = (row) => { return row[columnName] == null || string.IsNullOrEmpty(row[columnName].ToString()); };
                    Func<IDictionary<string, object>, bool> notFilter = (row) => { return row[columnName] != null && !string.IsNullOrEmpty(row[columnName].ToString()); };

                    if (not)
                        v.State.FilterFunctions.Push(notFilter);
                    else
                        v.State.FilterFunctions.Push(filter);
                }
            );

            return visitor;
        }
    }
}
