using System;

namespace Nova.Exceptions
{
    public class ScriptActionException : Exception
    {
        public ScriptActionException(string message, Exception innerException)
            : base(message, innerException) { }
    }
}