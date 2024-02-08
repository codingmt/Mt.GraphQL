using System;

namespace Mt.GraphQL.Internal
{
    public class InternalException : Exception
    {
        public InternalException(string message) : base(message)
        { }

        public InternalException(string message, Exception innerException) : base(message, innerException)
        { }
    }

    /// <summary>
    /// Exception thrown while parsing a query expression.
    /// </summary>
    public class QueryInternalException : InternalException
    {
        /// <summary>
        /// The query that yielded the <see cref="QueryInternalException"/>.
        /// </summary>
        public string Query { get; }

        /// <summary>
        /// Creates a new <see cref="QueryInternalException"/>.
        /// </summary>
        /// <param name="query">The query that yielded the <see cref="QueryInternalException"/>.</param>
        /// <param name="message">The message of the exception.</param>
        public QueryInternalException(string query, string message) : base(message)
        {
            Query = query;
        }

        /// <summary>
        /// Creates a new <see cref="QueryInternalException"/>.
        /// </summary>
        /// <param name="query">The query that yielded the <see cref="QueryInternalException"/>.</param>
        /// <param name="message">The message of the exception.</param>
        /// <param name="innerException">The inner exception.</param>
        public QueryInternalException(string query, string message, Exception innerException) : base(message, innerException)
        {
            Query = query;
        }
    }
}
