using System.Net.Http;
using System.Threading.Tasks;

namespace Mt.GraphQL.Api
{
    /// <summary>
    /// Base class for a GraphQL API client.
    /// </summary>
    public abstract class ClientBase
    {
        /// <summary>
        /// The API's configuration.
        /// </summary>
        public Configuration Configuration { get; }

        /// <summary>
        /// Creates a new API client.
        /// </summary>
        protected ClientBase()
        {
            Configuration = new Configuration();
        }

        /// <summary>
        /// Creates a new API client.
        /// </summary>
        /// <param name="configurationName">The prefix for the configuration items, separated by a colon.</param>
        protected ClientBase(string configurationName)
        {
            Configuration = new Configuration(configurationName);
        }

        /// <summary>
        /// Creates a new API client.
        /// </summary>
        /// <param name="configuration">The API's configuration.</param>
        protected ClientBase(Configuration configuration)
        {
            Configuration = configuration;
        }

        /// <summary>
        /// Creates a new <see cref="ClientQuery{T}"/> of type <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="T">The type of <see cref="Query{T}"/>.</typeparam>
        protected ClientQuery<T> CreateQuery<T>()
            where T : class
        {
            var result = new ClientQuery<T>();
            result.SetClient(this);
            return result;
        }

        internal async Task<string> FetchDataAsync(string entity, string query)
        {
            var request = 
                Configuration.CreateHttpRequestMessageHandler == null
                    ? CreateRequest(entity, HttpMethod.Get, query, null)
                    : await Configuration.CreateHttpRequestMessageHandler(Configuration, entity, HttpMethod.Get, query, null);
            return
                Configuration.ProcessRequestHandler == null
                    ? await ProcessRequestAsync(request)
                    : await Configuration.ProcessRequestHandler(Configuration, request);
        }

        private async Task<string> ProcessRequestAsync(HttpRequestMessage httpRequestMessage)
        {
            using (var clientLease = HttpClientPool.GetHttpClient())
            {
                var response = await clientLease.HttpClient.SendAsync(httpRequestMessage);
                if (!response.IsSuccessStatusCode)
                    throw new HttpStatusCodeException(response.StatusCode, response.ReasonPhrase, await response.Content.ReadAsStringAsync());

                return await response.Content.ReadAsStringAsync();
            }
        }

        private HttpRequestMessage CreateRequest(string entity, HttpMethod httpMethod, string query, string payload = null)
        {
            var request = new HttpRequestMessage(
                httpMethod,
                $"{Configuration.Url}/{entity}?{query}");
            if (!string.IsNullOrEmpty(Configuration.Key))
                request.Headers.Add(Configuration.KeyHeaderName, Configuration.Key);
            if (payload != null)
                request.Content = new StringContent(payload);
            return request;
        }
    }
}
