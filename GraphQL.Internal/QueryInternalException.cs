using System;

namespace Mt.GraphQL.Internal
{
    /// <summary>
    /// Exception thrown while parsing a query expression.
    /// </summary>
    public class QueryInternalException : Exception
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
