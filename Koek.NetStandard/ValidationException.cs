using System;

namespace Koek
{
    /// <summary>
    /// A generic validation exception.
    /// </summary>
    /// <remarks>
    /// The exception only carries one message about a validation failure. In theory, you might have an object with
    /// multiple invalid fields but reporting all such fields on an object is almost never necessary. Therefore,
    /// carrying multiple failures is not supported by this exception, for simplicity.
    /// </remarks>
    public class ValidationException : Exception
    {
        public ValidationException()
        {
        }

        public ValidationException(string message) : base(message)
        {
        }

        public ValidationException(string message, Exception inner) : base(message, inner)
        {
        }
    }
}