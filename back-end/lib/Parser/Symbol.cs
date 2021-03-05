using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Sara.Lib.Parser
{
    /// <summary>
    /// Defines a symbol in a production rule. A symbol can be a terminal (lexer / terminal symbol)
    /// or another production rule (non terminal).
    /// </summary>
    public class Symbol : ILoggable
    {
        public Symbol(string value, RuleType ruleType)
        {
            string name = value;
            string modifier = null;
            string[] parts = null;

            if (ruleType == RuleType.ParserRule)
            {
                // Check for rewrite rule (only parser rules)
                parts = name.Split(new string[] { ":" }, StringSplitOptions.None);
                if (parts.Length > 1)
                {
                    this.Alias = parts[0];
                    name = parts[1];
                }

                // modifiers
                var modifiers = new char[] { '+', '*', '?', '!' };

                if (modifiers.Contains(name.Last()))
                {
                    modifier = name.Substring(name.Length - 1, 1).First().ToString();
                    name = name.Substring(0, name.Length - 1);
                }
            }

            this.Name = name;
            if (parts == null || parts.Length == 1)
                this.Alias = this.Name;

            this.Optional = modifier == "?" || modifier == "*";
            this.Many = modifier == "+" || modifier == "*";
            this.Ignore = modifier == "!";
        }

        /// <summary>
        /// Optional alias. If not set, then equivalent to the Name property. Used to name child properties in the abstract syntax tree.
        /// </summary>
        public string Alias { get; set; }

        /// <summary>
        /// Name of the symbol.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Set to true if the symbol is optional in the syntax.
        /// </summary>
        public bool Optional { get; set; }

        /// <summary>
        /// Set to true if symbol allows multiple values (list).
        /// </summary>
        public bool Many { get; set; }

        /// <summary>
        /// Set to true if symbol to be ignored in the abstract syntax tree.
        /// </summary>
        public bool Ignore { get; set; }

        public Action<object, LogArgs> LogHandler { get; set; }

        /// <summary>
        /// Matches symbol to input.
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public MatchResult Match(string input)
        {
            var pat = string.Format(@"\A[\s]*(?<match>({0}))(?<remainder>([\s\S]*))[\s]*\Z", Name);
            Regex re = new Regex(pat, RegexOptions.IgnoreCase | RegexOptions.Multiline);
            var match = re.Match(input);
            return new MatchResult
            {
                Success = match.Success,
                Matched = match.Groups["match"].Value,
                Remainder = match.Groups["remainder"].Value
            };
        }

        /// <summary>
        /// Checks whether the input matches this symbol.
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public bool IsMatch(string input)
        {
            var pat = string.Format(@"\A[\s]*(?<match>({0}))(?<remainder>([\s\S]*))[\s]*\Z", Name);
            Regex re = new Regex(pat, RegexOptions.IgnoreCase | RegexOptions.Multiline);
            return re.IsMatch(input);
        }

        /// <summary>
        /// Checks whether the current symbol matches, and can read the input.
        /// If successful, the successful input is returned in the context.
        /// </summary>
        /// <param name="context">The parser context.</param>
        /// <returns>True if successful. The abstract syntax tree is constructed using the context.Results object.</returns>
        public bool Parse(ParserContext context)
        {
            LogHandler?.Invoke(this, new LogArgs
            {
                LogType = LogType.BEGIN,
                NestingLevel = context.CurrentProductionRule.Count(),
                Message = $"Token Index: {context.CurrentTokenIndex}, Results: {context.Results.Count()}, Symbol={this.Name}, Next Token=[{context.PeekToken().TokenName} - \"{context.PeekToken().TokenValue}\"]"
            });

            // save token position
            int temp = context.CurrentTokenIndex;
            bool ok = false;
            var once = false;

            if (this.Optional && context.TokenEOF)
            {
                return true;
            }
            else
            {
                while (true)
                {
                    Token token = null;
                    if (!context.TokenEOF)
                    {
                        token = context.TryToken(this.Name);
                    }

                    if (token != null)
                    {
                        // terminal
                        ok = true;
                        if (!this.Ignore)
                            context.UpdateResult(this.Alias, token);
                    }
                    // check to see if the symbol a pointer to another production rule?
                    // if so, add new item onto stack.
                    else
                    {
                        // non terminal
                        var rules = context
                            .ProductionRules
                            .Where(r => r.RuleType == RuleType.ParserRule)
                            .Where(r => r.Name.Equals(Name, StringComparison.OrdinalIgnoreCase));

                        if (!rules.Any())
                            break;

                        foreach (var rule in rules)
                        {
                            rule.LogHandler = this.LogHandler;
                            object obj = null;
                            ok = rule.Parse(context, out obj);
                            if (ok)
                            {
                                if (!this.Ignore)
                                    context.UpdateResult(this.Alias, obj);
                                break;
                            }
                        }
                    }

                    // wind back the token index if the symbol did not match tokens.
                    if (ok)
                    {
                        once = true;
                        if (!Many)
                            break;
                    }
                    else
                    {
                        if (!once)
                            context.CurrentTokenIndex = temp;
                        break;
                    }
                }
            }

            // return true if match (at least once).
            var success = ok || once || Optional;

            LogHandler?.Invoke(this, new LogArgs
            {
                LogType = success ? LogType.SUCCESS : LogType.FAILURE,
                NestingLevel = context.CurrentProductionRule.Count(),
                Message = $"Token Index: {context.CurrentTokenIndex}, Results: {context.Results.Count()}, Symbol={this.Name}, Next Token=[{context.PeekToken().TokenName} - \"{context.PeekToken().TokenValue}\"]"
            });

            return success;
        }

        public override string ToString()
        {
            var alias = Alias != Name ? $"{Alias}:" : "";
            var modifier = Ignore ? "!" : (Optional && !Many ? "?" : (Optional && Many ? "*" : (Many ? "+" : "")));
            return $"{alias}{Name}{modifier}";
        }
    }
}