using Sara.Lib.Parser;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Threading.Tasks;

namespace Sara.Lib.Data.Parsers
{
    /// <summary>
    /// Parser used for the SELECT part of the Data url.
    /// </summary>
    public abstract class SelectParser
    {
        public dynamic Parse(string input, IEnumerable<string> columns)
        {
            Sara.Lib.Parser.Parser parser = new Sara.Lib.Parser.Parser(Grammar, "select_expr");
            var selectNode = parser.Parse(input);
            var visitor = GetVisitor(columns);
            var state = parser.Execute(selectNode, visitor);
            return state;
        }

        private string Grammar => @"
COMMA               =   ""[,]"";
AGGREGATE_FUNCTION  =   ""\bSUM\b"";
AGGREGATE_FUNCTION  =   ""\bCOUNT\b"";
AGGREGATE_FUNCTION  =   ""\bMIN\b"";
AGGREGATE_FUNCTION  =   ""\bMAX\b"";
AGGREGATE_FUNCTION  =   ""\bAVG\b"";
BIN_FUNCTION        =   ""\bBIN\b"";
IDENTIFIER          =   ""\b[A-Z_][A-Z_0-9]*\b"";
NUMBER              =   ""\b\d+(\.\d+)?\b"";
LEFT_PAREN          =   ""[(]"";
RIGHT_PAREN         =   ""[)]"";
select_term         =   ID:IDENTIFIER, ALIAS:IDENTIFIER? ;
select_term         =   FN:AGGREGATE_FUNCTION, LEFT_PAREN!, ID:IDENTIFIER, RIGHT_PAREN!, ALIAS:IDENTIFIER? ;
select_term         =   FN:BIN_FUNCTION, LEFT_PAREN!, ID:IDENTIFIER, COMMA!, PARAM:NUMBER, RIGHT_PAREN!, ALIAS:IDENTIFIER? ;
select_factor       =   COMMA!, :select_term ;
select_expr         =   SELECT:select_term, SELECT:select_factor* ;
";

        protected abstract Visitor GetVisitor(IEnumerable<string> columns);
    }
}
