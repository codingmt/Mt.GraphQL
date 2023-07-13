using System;

namespace Mt.GraphQL.Internal
{
    /// <summary>
    /// Exception thrown while parsing a query expression.
    /// </summary>
    public class QueryParseException : Exception
    {
        /// <summary>
        /// The query that yielded the <see cref="QueryParseException"/>.
        /// </summary>
        public string Query { get; }

        /// <summary>
        /// Creates a new <see cref="QueryParseException"/>.
        /// </summary>
        /// <param name="query">The query that yielded the <see cref="QueryParseException"/>.</param>
        /// <param name="message">The message of the exception.</param>
        public QueryParseException(string query, string message) : base(message)
        {
            Query = query;
        }

        /// <summary>
        /// Creates a new <see cref="QueryParseException"/>.
        /// </summary>
        /// <param name="query">The query that yielded the <see cref="QueryParseException"/>.</param>
        /// <param name="message">The message of the exception.</param>
        /// <param name="innerException">The inner exception.</param>
        public QueryParseException(string query, string message, Exception innerException) : base(message, innerException)
        {
            Query = query;
        }
    }
}
