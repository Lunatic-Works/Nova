using System;

namespace Nova.Exceptions
{
    public class ScriptActionException : Exception
    {
        public ScriptActionException() { }
        public ScriptActionException(string message) : base(message) { }
        public ScriptActionException(string message, Exception inner) : base(message, inner) { }
    }
}
