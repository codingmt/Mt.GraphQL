using System;
using System.Net;

namespace Mt.GraphQL.Api
{
    /// <summary>
    /// Thrown when an unsuccesfull status code is received.
    /// </summary>
    public class HttpStatusCodeException : Exception
    {
        /// <summary>
        /// The received status code.
        /// </summary>
        public HttpStatusCode StatusCode { get; set; }

        /// <summary>
        /// The received reason phrase.
        /// </summary>
        public string ReasonPhrase { get; set; }

        /// <summary>
        /// The received content.
        /// </summary>
        public string Content { get; set; }

        /// <summary>
        /// Creates a new <see cref="HttpStatusCodeException"/>.
        /// </summary>
        /// <param name="statusCode">The received status code.</param>
        /// <param name="reasonPhrase">The received reason phrase.</param>
        /// <param name="content">The received content.</param>
        public HttpStatusCodeException(HttpStatusCode statusCode, string reasonPhrase, string content)
            : base($"Error processing HTTP request: {statusCode} {reasonPhrase}".Trim())
        {
            StatusCode = statusCode;
            ReasonPhrase = reasonPhrase;
            Content = content;
        }
    }
}
