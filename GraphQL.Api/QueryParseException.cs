using System;

namespace Mt.GraphQL.Api
{
    public class QueryParseException : Exception
    {
        public string Query { get; }

        public QueryParseException(string query, string message) : base(message)
        {
            Query = query;
        }

        public QueryParseException(string query, string message, Exception innerException) : base(message, innerException)
        {
            Query = query;
        }
    }
}
