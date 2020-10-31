using System;

namespace Nova.Exceptions
{
    public class DuplicatedDefinitionException : ArgumentException
    {
        public DuplicatedDefinitionException(string message, Exception innerException)
            : base(message, innerException) { }

        public DuplicatedDefinitionException(string message)
            : base(message) { }
    }
}