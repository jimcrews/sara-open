using System;
using System.Collections.Generic;
using System.Text;

namespace Sara.Lib.Csv
{
    /// <summary>
    /// Provides parsing functions for a string similar to StreamReader
    /// </summary>
    public class StringParser
    {
        char[] innerString = null;
        int position = -1;

        /// <summary>
        /// Matches a character.
        /// </summary>
        /// <param name="token"></param>
        /// <returns>Returns true and advances the cursor if match.</returns>
        public bool Match(char? token)
        {
            if (!token.HasValue)
                return false;

            if (Peek() == token)
            {
                Read();
                return true;
            }
            else
                return false;
        }

        public bool IsWhiteSpaceChar()
        {
            return char.IsWhiteSpace(Peek().Value);
        }

        /// <summary>
        /// Reads a character from the string, and advances the position.
        /// </summary>
        /// <returns></returns>
        public char? Read()
        {
            if (EndOfString)
                return null;
            else
                return innerString[++position];
        }

        public char? Peek()
        {
            if (EndOfString)
                return null;
            else
                return innerString[position + 1];
        }

        public bool StartOfString
        {
            get { return position == -1; }
        }

        public bool EndOfString
        {
            get
            {
                return !(position < innerString.Length - 1 && innerString.Length > 0);
            }
        }

        public StringParser(string s)
        {
            innerString = s.ToCharArray();
            position = -1;
        }
    }
}