using System;

namespace Injection
{
    /// <summary>
    /// Exceptions that is thrown when something goes wrong while working with the injectionist
    /// </summary>
    public class ResolutionException : Exception
    {
        /// <summary>
        /// Constructs the exception
        /// </summary>
        public ResolutionException(string message, params object[] objs)
            : base(string.Format(message, objs))
        {
        }

        /// <summary>
        /// Constructs the exception
        /// </summary>
        public ResolutionException(Exception innerException, string message, params object[] objs)
            : base(string.Format(message, objs), innerException)
        {
        }
    }
}