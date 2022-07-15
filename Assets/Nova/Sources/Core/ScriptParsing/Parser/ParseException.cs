using System;
using System.Runtime.Serialization;

namespace Nova.Script
{
    [Serializable]
    public class ParseException : Exception
    {
        public ParseException() { }
        public ParseException(string message) : base(message) { }
        public ParseException(string message, Exception inner) : base(message, inner) { }
        protected ParseException(SerializationInfo info, StreamingContext context) : base(info, context) { }
        public ParseException(Token token, string msg) : this(ErrorMessage(token, msg)) { }

        public static string ErrorMessage(Token token, string msg)
        {
            return ErrorMessage(token.line, token.column, msg);
        }

        public static string ErrorMessage(int line, int column, string msg)
        {
            return $"Line {line}, Column {column}: {msg}";
        }

        public static void ExpectToken(Token token, TokenType type, string display)
        {
            if (token.type != type)
            {
                throw new ParseException(ErrorMessage(token, $"Expect {display}"));
            }
        }

        // TODO: params?
        public static void ExpectToken(Token token, TokenType typeA, TokenType typeB, string display)
        {
            if (token.type != typeA && token.type != typeB)
            {
                throw new ParseException(ErrorMessage(token, $"Expect {display}"));
            }
        }
    }
}
