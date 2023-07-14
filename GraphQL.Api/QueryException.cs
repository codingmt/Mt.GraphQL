using Mt.GraphQL.Internal;
using System;

namespace Mt.GraphQL.Api
{
    /// <summary>
    /// Exception thrown while parsing a query expression.
    /// </summary>
    public class QueryException : Exception
    {
        /// <summary>
        /// The query that yielded the <see cref="QueryException"/>.
        /// </summary>
        public string Query { get; }

        /// <summary>
        /// Creates a new <see cref="QueryException"/>.
        /// </summary>
        /// <param name="query">The query that yielded the <see cref="QueryException"/>.</param>
        /// <param name="message">The message of the exception.</param>
        public QueryException(string query, string message) : base(message)
        {
            Query = query;
        }

        /// <summary>
        /// Creates a new <see cref="QueryInternalException"/>.
        /// </summary>
        /// <param name="query">The query that yielded the <see cref="QueryInternalException"/>.</param>
        /// <param name="message">The message of the exception.</param>
        /// <param name="innerException">The inner exception.</param>
        public QueryException(string query, string message, Exception innerException) : base(message, innerException)
        {
            Query = query;
        }

        /// <summary>
        /// Creates a new <see cref="QueryException"/> from a <see cref="QueryInternalException"/>.
        /// </summary>
        public QueryException(QueryInternalException internalException) : base(internalException.Message, internalException.InnerException)
        { 
            Query = internalException.Query;
        }
    }
}
