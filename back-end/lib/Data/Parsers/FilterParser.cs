using Sara.Lib.Parser;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Threading.Tasks;

namespace Sara.Lib.Data.Parsers
{
    public enum ParamsType
    {
        FILTER,
        PARAMS
    }

    public abstract class FilterParser
    {
        /// <summary>
        /// Parses the filter query text passed in by the user, and constructs a valid SQL
        /// statement with SqlParameters correctly set up. Uses simple grammar to parse
        /// input.
        /// </summary>
        /// <param name="paramsType">PARAMS or FILTER</param>
        /// <param name="input">The input to parse</param>
        /// <param name="columns">The available columns in the dataset</param>
        /// <param name="initialState">Any initial state</param>
        /// <returns></returns>
        public dynamic Parse(ParamsType paramsType, string input, IEnumerable<string> columns, dynamic initialState = null)
        {
            Sara.Lib.Parser.Parser parser = new Sara.Lib.Parser.Parser(GetGrammar(paramsType), "search_condition");
            var filterNode = parser.Parse(input);
            var visitor = GetVisitor(paramsType, columns, initialState);
            var state = parser.Execute(filterNode, visitor);
            return state;
        }

        public string GetGrammar(ParamsType paramsType)
        {
            return @"
/* Grammar for FILTER query */
AND                 =   ""\bAND\b"";
OR                  =   ""\bOR\b"";
EQ_OP               =   ""\bEQ\b"";
NE_OP               =   ""\bNE\b"";
LT_OP               =   ""\bLT\b"";
LE_OP               =   ""\bLE\b"";
GT_OP               =   ""\bGT\b"";
GE_OP               =   ""\bGE\b"";
LEFT_PAREN          =   ""[(]"";
RIGHT_PAREN         =   ""[)]"";
COMMA               =   ""[,]"";
IN                  =   ""\b(IN)\b"";
CONTAINS            =   ""\bCONTAINS\b"";
BETWEEN             =   ""\bBETWEEN\b"";
ISBLANK             =   ""\bISBLANK\b"";
NOT                 =   ""\bNOT\b"";

LITERAL_STRING      =   ""['][^']*[']"";
LITERAL_NUMBER      =   ""[+-]?((\d+(\.\d*)?)|(\.\d+))"";
IDENTIFIER          =   ""[A-Z_][A-Z_0-9]*"";

comparison_operator =   :EQ_OP;
comparison_operator =   :NE_OP;
comparison_operator =   :LT_OP;
comparison_operator =   :LE_OP;
comparison_operator =   :GT_OP;
comparison_operator =   :GE_OP;

comparison_operand  =   :LITERAL_STRING;
comparison_operand  =   :LITERAL_NUMBER;
comparison_operand  =   :IDENTIFIER;

comparison_predicate    =   LHV:comparison_operand, OPERATOR:comparison_operator, RHV:comparison_operand;
in_factor               =   COMMA!, :comparison_operand;
in_predicate            =   LHV:comparison_operand, NOT:NOT?, IN!, LEFT_PAREN!, RHV:comparison_operand, RHV:in_factor*, RIGHT_PAREN!;
between_predicate       =   LHV:comparison_operand, NOT:NOT?, BETWEEN!, OP1:comparison_operand, AND!, OP2:comparison_operand;
contains_predicate      =   LHV:comparison_operand, NOT:NOT?, CONTAINS!, RHV:comparison_operand;
blank_predicate         =   LHV:comparison_operand, NOT:NOT?, ISBLANK;

predicate               =   :comparison_predicate;
predicate               =   :in_predicate;
predicate               =   :between_predicate;
predicate               =   :contains_predicate;
predicate               =   :blank_predicate;

boolean_primary         =   :predicate;

boolean_factor          =   AND!, :boolean_primary;
boolean_term            =   AND:boolean_primary, AND:boolean_factor*;

search_factor           =   OR!, :boolean_term;
search_condition        =   OR:boolean_term, OR:search_factor*;" + (paramsType == ParamsType.FILTER ? @"
boolean_primary         =   LEFT_PAREN!, CONDITION:search_condition, RIGHT_PAREN!;" : "");
        }

        protected static Dictionary<string, string> AllowedOperators(ParamsType paramsType)
        {
            if (paramsType == ParamsType.FILTER)
            {
                return new Dictionary<string, string>()
            {
                {"EQ_OP", "="},
                {"NE_OP", "<>"},
                {"GT_OP", ">"},
                {"GE_OP", ">="},
                {"LT_OP", "<"},
                {"LE_OP", "<="}
            };
            }
            else
            {
                return new Dictionary<string, string>()
            {
                {"EQ_OP", "="}
            };

            }
        }

        /// <summary>
        /// Implements the concrete abstract syntax tree walker.
        /// </summary>
        /// <param name="paramsType"></param>
        /// <param name="columns"></param>
        /// <param name="initialState"></param>
        /// <returns></returns>
        protected abstract Visitor GetVisitor(ParamsType paramsType, IEnumerable<string> columns, dynamic initialState = null);

    }
}

