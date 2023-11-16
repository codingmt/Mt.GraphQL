using Microsoft.AspNetCore.Mvc.Testing;
using Mt.GraphQL.Api.Test.Web.Core.Models;

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
        public async Task TestGetAllManyToOne()
        {
            var result = await _client.Contacts.ToArrayAsync();

            Assert.That(
                _client.Json, 
                Is.EqualTo("[{\"id\":1,\"name\":\"Contact 1.1\",\"function\":\"CEO\",\"isAuthorizedToSign\":true,\"dateOfBirth\":\"1970-05-15\",\"customer\":{\"id\":1,\"name\":\"Customer 1\"}},{\"id\":2,\"name\":\"Contact 1.2\",\"function\":\"Secretary\",\"isAuthorizedToSign\":false,\"dateOfBirth\":\"1980-06-16\",\"customer\":{\"id\":1,\"name\":\"Customer 1\"}},{\"id\":3,\"name\":\"Contact 1.3\",\"function\":\"Sales Mgr\",\"isAuthorizedToSign\":false,\"dateOfBirth\":\"1990-07-17\",\"customer\":{\"id\":1,\"name\":\"Customer 1\"}},{\"id\":4,\"name\":\"Contact 2.1\",\"function\":\"CEO\",\"isAuthorizedToSign\":true,\"dateOfBirth\":\"1971-05-18\",\"customer\":{\"id\":2,\"name\":\"Customer 2\"}}]"));

            Assert.That(result, Is.Not.Null);
            Assert.That(result, Has.Length.EqualTo(4));
        }

        [Test]
        public async Task TestGetAllOneToMany()
        {
            var result = await _client.Customers.ToArrayAsync();

            Assert.That(
                _client.Json, 
                Is.EqualTo("[{\"id\":1,\"name\":\"Customer 1\",\"contacts\":[{\"id\":1,\"name\":\"Contact 1.1\",\"function\":\"CEO\",\"isAuthorizedToSign\":true,\"dateOfBirth\":\"1970-05-15\"},{\"id\":2,\"name\":\"Contact 1.2\",\"function\":\"Secretary\",\"isAuthorizedToSign\":false,\"dateOfBirth\":\"1980-06-16\"},{\"id\":3,\"name\":\"Contact 1.3\",\"function\":\"Sales Mgr\",\"isAuthorizedToSign\":false,\"dateOfBirth\":\"1990-07-17\"}]},{\"id\":2,\"name\":\"Customer 2\",\"contacts\":[{\"id\":4,\"name\":\"Contact 2.1\",\"function\":\"CEO\",\"isAuthorizedToSign\":true,\"dateOfBirth\":\"1971-05-18\"}]},{\"id\":3,\"name\":\"Customer 3\",\"contacts\":[]}]"));

            Assert.That(result, Is.Not.Null);
            Assert.That(result, Has.Length.EqualTo(3));
        }

        [Test]
        public async Task TestGetSelection()
        {
            var result = await _client.Contacts
                .Select(x => new { x.Id, x.Name, x.DateOfBirth })
                .Where(x => x.Name.StartsWith("Contact 1"))
                .OrderByDescending(x => x.Name)
                .Skip(1)
                .ToArrayAsync();

            Assert.That(
                _client.Json, 
                Is.EqualTo("[{\"id\":2,\"name\":\"Contact 1.2\",\"dateOfBirth\":\"1980-06-16\"},{\"id\":1,\"name\":\"Contact 1.1\",\"dateOfBirth\":\"1970-05-15\"}]"));

            Assert.That(result, Is.Not.Null);
            Assert.That(result, Has.Length.EqualTo(2));
        }

        [Test]
        public async Task TestSingleProperty()
        {
            var result = await _client.Contacts
                .Select(x => x.Id)
                .Where(x => x.Customer_Id != 2)
                .OrderByDescending(x => x.Name)
                .Skip(1)
                .ToArrayAsync();

            Assert.That(_client.Json, Is.EqualTo(@"[2,1]"));

            Assert.That(result, Is.Not.Null);
            Assert.That(result, Has.Length.EqualTo(2));
            Assert.That(result[0], Is.EqualTo(2));
            Assert.That(result[1], Is.EqualTo(1));
        }

        [Test]
        public async Task TestUnorderedError()
        {
            Exception? e = null;
            try
            {
                await _client.Customers.Take(1).ToArrayAsync();
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
                await _client.Contacts.OrderBy(x => x.Function).ToArrayAsync();
            }
            catch (Exception ex)
            {
                e = ex;
            }

            Assert.That(e, Is.Not.Null);
            Assert.That(e, Is.TypeOf<HttpStatusCodeException>());
            Assert.That(e, Has.Message.EqualTo("HTTP BadRequest: Error in OrderBy: Column Contact.Function cannot be used for filtering and ordering."));
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

            public ClientQuery<Customer> Customers => CreateQuery<Customer>();

            public ClientQuery<Contact> Contacts => CreateQuery<Contact>();
        }
    }
}
