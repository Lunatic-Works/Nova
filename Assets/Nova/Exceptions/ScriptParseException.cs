using System;

namespace Nova.Exceptions
{
    public class ScriptParseException : Exception
    {
        public ScriptParseException(string message)
            : base(message) { }

        public ScriptParseException(string message, Exception innerException)
            : base(message, innerException) { }
    }
}