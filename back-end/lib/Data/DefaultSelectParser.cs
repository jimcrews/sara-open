using Sara.Lib.Data.Parsers;
using Sara.Lib.Parser;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Text;
using System.Linq;
using System.Data;
using DotLiquid.Util;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Text.RegularExpressions;

namespace Sara.Lib.Data
{
    public class ColumnNode
    {
        /// <summary>
        /// The column alias
        /// </summary>
        public string Alias { get; set; }
        
        /// <summary>
        /// The source column name. Don't modify this
        /// </summary>
        public string ColumnName { get; set; }
        
        /// <summary>
        /// A formatted string. This can be manipulated, and can be any provider-specific (e.g. SQL) string
        /// </summary>
        public string FormattedName { get; set; }
        
        /// <summary>
        /// The function name
        /// </summary>
        public string Function { get; set; }
        
        /// <summary>
        /// the parameter provided
        /// </summary>
        public string Parameter { get; set; }
        
        /// <summary>
        /// Set to true if the column is grouped.
        /// </summary>
        /// <returns></returns>
        public bool IsAggregateFunction()
        {
            return new List<string>() { "MIN", "MAX", "SUM", "COUNT", "AVG" }.Contains(Function);
        }
    }

    public class DefaultSelectParser : SelectParser
    {
        protected override Visitor GetVisitor(IEnumerable<string> columns)
        {
            // Initial state
            dynamic state = new ExpandoObject();
            Func<bool> IsGroupedFn = () => ((List<ColumnNode>)state.Result).Any(r => r.IsAggregateFunction());
            state.IsGrouped = IsGroupedFn;
            state.Result = new List<ColumnNode>();
            state.Columns = columns;    // Passed in - the full list of available columns. Used to perform validation.

            var visitor = new Visitor(state);

            visitor.AddVisitor(
                "select_expr",
                (v, n) =>
                {
                    // Inspect the node.Properties["SELECT"]
                    // This will contain all the fields in a collection
                    // If type of item = Token, then simple column / group by column
                    // If type of item = Node, then aggregate column (sum, count etc)
                    foreach (var item in (IEnumerable<Object>)n.Properties["SELECT"])
                    {
                        var itemAsNode = item as Node;
                        string name;

                        if (itemAsNode == null)
                        {
                            throw new Exception("Expecting a node here.");
                        }

                        // this is a aggregate node (sum, count etc)
                        name = ((Token)itemAsNode.Properties["ID"]).TokenValue;

                        string fn = null;
                        if (((Node)itemAsNode).Properties.ContainsKey("FN"))
                        {
                            fn = ((Token)itemAsNode.Properties["FN"]).TokenValue;
                        }

                        string param = null;
                        if (((Node)itemAsNode).Properties.ContainsKey("PARAM"))
                        {
                            param = ((Token)itemAsNode.Properties["PARAM"]).TokenValue;
                        }

                        string alias = null;
                        if (((Node)itemAsNode).Properties.ContainsKey("ALIAS"))
                        {
                            alias = ((Token)itemAsNode.Properties["ALIAS"]).TokenValue;
                        }

                        // check column name is valid
                        // also alter names to match server meta data.
                        var matchedColumn = ((IEnumerable<string>)v.State.Columns).FirstOrDefault(c => c.Equals(name, StringComparison.OrdinalIgnoreCase));

                        if (matchedColumn == null)
                        {
                            throw new Exception($"Column '{name}' does not exist!");
                        }

                        v.State.Result.Add(new ColumnNode()
                        {
                            ColumnName = matchedColumn,
                            Alias = !string.IsNullOrEmpty(alias) ? alias : matchedColumn,
                            FormattedName = matchedColumn,
                            Function = fn,
                            Parameter = param
                        });
                    }
                }
            );

            return visitor;
        }
    }
}
