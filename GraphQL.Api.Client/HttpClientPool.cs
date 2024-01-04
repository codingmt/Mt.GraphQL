using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;

namespace Mt.GraphQL.Api
{
    internal static class HttpClientPool
    {
        private static readonly object _lock = new object();

        private static readonly List<HttpClient> _httpClients = new List<HttpClient>();

        internal static HttpClientLease GetHttpClient()
        {
            HttpClient client = null;

            lock (_lock)
            {
                if (_httpClients.Any())
                {
                    client = _httpClients[0];
                    _httpClients.RemoveAt(0);
                }
            }

            return new HttpClientLease(client ?? new HttpClient());
        }

        internal class HttpClientLease : IDisposable
        {
            public HttpClient HttpClient { get; }

            public HttpClientLease(HttpClient httpClient)
            {
                HttpClient = httpClient;
            }

            public void Dispose()
            {
                lock( _lock)
                {
                    _httpClients.Add(HttpClient);
                }
            }
        }
    }
}
