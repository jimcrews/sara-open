

namespace Sara.Lib.Parser
{
    /// <summary>
    /// A terminal token within the input. The lexer converts string input into an array of lexed tokens.
    /// </summary>
    public class Token
    {
        /// <summary>
        /// Describes the token.
        /// </summary>
        public string TokenName { get; set; }

        /// <summary>
        /// The actual value of the token.
        /// </summary>
        public string TokenValue { get; set; }
    }
}