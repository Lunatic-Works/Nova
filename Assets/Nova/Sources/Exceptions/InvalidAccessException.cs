using System;

namespace Nova.Exceptions
{
    /// <summary>
    /// Used when something is not found and it is not an argument.
    /// </summary>
    public class InvalidAccessException : Exception
    {
        public InvalidAccessException(string message, Exception innerException)
            : base(message, innerException) { }

        public InvalidAccessException(string message)
            : base(message) { }
    }
}