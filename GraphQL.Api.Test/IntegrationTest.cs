using Microsoft.AspNetCore.Mvc.Testing;

namespace Mt.GraphQL.Api.Test
{
    public class IntegrationTest
    {
        private TestWebApp _application;
        private HttpClient _httpClient;
        private TestClient _client;

        [SetUp]
        public void SetUp()
        {
            _application = new TestWebApp();
            _httpClient = _application.CreateClient();
            _client = new TestClient(_httpClient);
        }

        [TearDown]
        public void TearDown()
        {
            _httpClient.Dispose();
            _application.Dispose();
        }

        [Test]
        public async Task TestSingleProperty()
        {
            var result = await _client.Set
                .Select(x => x.Id)
                .Where(x => x.Related_Id != null)
                .OrderByDescending(x => x.Name)
                .Skip(1)
                .ToArrayAsync();

            Assert.That(_client.Json, Is.EqualTo(@"[
  10,
  8,
  7,
  6
]"));

            Assert.That(result, Is.Not.Null);
            Assert.That(result, Has.Length.EqualTo(4));
            Assert.That(result[0], Is.EqualTo(10));
            Assert.That(result[1], Is.EqualTo(8));
            Assert.That(result[2], Is.EqualTo(7));
            Assert.That(result[3], Is.EqualTo(6));
        }

        [Test]
        public async Task TestUnorderedError()
        {
            Exception? e = null;
            try
            {
                await _client.Set.Take(1).ToArrayAsync();
            }
            catch (Exception ex)
            {
                e = ex;
            }

            Assert.That(e, Is.Not.Null);
            Assert.That(e, Is.TypeOf<HttpStatusCodeException>());
            Assert.That(e, Has.Message.EqualTo("HTTP BadRequest: You cannot use Skip or Take without OrderBy or OrderByDescending."));
        }

        [Test]
        public async Task TestNotIndexedError()
        {
            Exception? e = null;
            try
            {
                await _client.Set.OrderBy(x => x.Type).ToArrayAsync();
            }
            catch (Exception ex)
            {
                e = ex;
            }

            Assert.That(e, Is.Not.Null);
            Assert.That(e, Is.TypeOf<HttpStatusCodeException>());
            Assert.That(e, Has.Message.EqualTo("HTTP BadRequest: Error in OrderBy: Column Entity.Type cannot be used for filtering and ordering."));
        }

        private class TestWebApp : WebApplicationFactory<Web.Core.Program>
        { }

        private class TestClient : ClientBase
        {
            private readonly HttpClient _httpClient;

            public string? Json { get; private set; }

            public TestClient(HttpClient httpClient)
            {
                Configuration.Url = string.Empty;
                Configuration.ProcessRequestHandler = ProcessRequest;
                _httpClient = httpClient;
            }

            private async Task<string> ProcessRequest(Configuration configuration, HttpRequestMessage httpRequestMessage)
            {
                var response = await _httpClient.SendAsync(httpRequestMessage);
                if (!response.IsSuccessStatusCode)
                    throw new HttpStatusCodeException(response.StatusCode, response.ReasonPhrase, await response.Content.ReadAsStringAsync());

                Json = await response.Content.ReadAsStringAsync();

                return Json;
            }

            public ClientQuery<Entity> Set => CreateQuery<Entity>();
        }
    }
}
