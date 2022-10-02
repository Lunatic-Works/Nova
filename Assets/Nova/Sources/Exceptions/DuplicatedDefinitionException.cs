using System;

namespace Nova.Exceptions
{
    public class DuplicatedDefinitionException : ArgumentException
    {
        public DuplicatedDefinitionException() { }
        public DuplicatedDefinitionException(string message) : base(message) { }
        public DuplicatedDefinitionException(string message, Exception inner) : base(message, inner) { }
    }
}
