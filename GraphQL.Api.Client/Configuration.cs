using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace Mt.GraphQL.Api
{
    /// <summary>
    /// Client configuration
    /// </summary>
    public class Configuration
    {
        /// <summary>
        /// Delegate for creating a custom <see cref="HttpRequestMessage"/>.
        /// </summary>
        /// <param name="configuration">This <see cref="Configuration"/>.</param>
        /// <param name="entityName">The name of the entity.</param>
        /// <param name="httpMethod">The method to create a request message for.</param>
        /// <param name="query">The query string.</param>
        /// <param name="payload">The optional payload.</param>
        /// <returns>The created <see cref="HttpRequestMessage"/>.</returns>
        public delegate Task<HttpRequestMessage> CreateHttpRequestMessageAsyncDelegate(Configuration configuration, string entityName, HttpMethod httpMethod, string query, string? payload = null);
        /// <summary>
        /// Delegate for processing a <see cref="HttpRequestMessage"/>.
        /// </summary>
        /// <param name="configuration">This <see cref="Configuration"/>.</param>
        /// <param name="httpRequestMessage"></param>
        /// <returns>The response body.</returns>
        public delegate Task<string> ProcessRequestAsyncDelegate(Configuration configuration, HttpRequestMessage httpRequestMessage);

        /// <summary>
        /// Creates a new <see cref="Configuration"/>.
        /// </summary>
        public Configuration() : this(string.Empty)
        { }

        /// <summary>
        /// Creates a new <see cref="Configuration"/>.
        /// <param name="configurationName">The name used to prefix the configuration names, separated by a colon.</param>
        /// </summary>
        public Configuration(string configurationName)
        {
            if (string.IsNullOrEmpty(configurationName))
                configurationName = string.Empty;
            else
                configurationName = $"{configurationName}:";

            Url = Environment.GetEnvironmentVariable(configurationName + "ApiUrl");
            Key = Environment.GetEnvironmentVariable(configurationName + "ApiKey");
            KeyHeaderName = Environment.GetEnvironmentVariable(configurationName + "ApiKeyHeaderName") ?? KeyHeaderName;
        }

        /// <summary>
        /// The URL to access the API.
        /// </summary>
        public string Url { get; set; }
        /// <summary>
        /// The Key for accessing the API.
        /// </summary>
        public string Key { get; set; }
        /// <summary>
        /// The name of the HTTP header for passing the <see cref="Key"/>.
        /// </summary>
        public string KeyHeaderName { get; set; } = "Api_Key";

        /// <summary>
        /// Optional handler for creating a custom <see cref="HttpRequestMessage"/>.
        /// </summary>
        public CreateHttpRequestMessageAsyncDelegate? CreateHttpRequestMessageHandler { get; set; }
        /// <summary>
        /// Optional handler for custom processing the <see cref="HttpRequestMessage"/>.
        /// </summary>
        public ProcessRequestAsyncDelegate? ProcessRequestHandler { get; set; }
    }
}
