using Microsoft.AspNetCore.Mvc.Testing;
using Mt.GraphQL.Api.Server;
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
                Is.EqualTo(@"{
  ""query"": {
    ""orderBy"": ""Id"",
    ""take"": 200
  },
  ""data"": [
    {
      ""id"": 1,
      ""name"": ""Contact 1.1"",
      ""function"": ""CEO"",
      ""isAuthorizedToSign"": true,
      ""dateOfBirth"": ""1970-05-15""
    },
    {
      ""id"": 2,
      ""name"": ""Contact 1.2"",
      ""function"": ""Secretary"",
      ""isAuthorizedToSign"": false,
      ""dateOfBirth"": ""1980-06-16""
    },
    {
      ""id"": 3,
      ""name"": ""Contact 1.3"",
      ""function"": ""Sales Mgr"",
      ""isAuthorizedToSign"": false,
      ""dateOfBirth"": ""1990-07-17""
    },
    {
      ""id"": 4,
      ""name"": ""Contact 2.1"",
      ""function"": ""CEO"",
      ""isAuthorizedToSign"": true,
      ""dateOfBirth"": ""1971-05-18""
    }
  ]
}"));

            Assert.That(result, Is.Not.Null);
            Assert.That(result, Has.Length.EqualTo(4));
        }

        [Test]
        public async Task TestGetAllOneToMany()
        {
            var result = await _client.Customers.ToArrayAsync();

            Assert.That(
                _client.Json, 
                Is.EqualTo(@"{
  ""query"": {
    ""orderBy"": ""Id"",
    ""take"": 200
  },
  ""data"": [
    {
      ""id"": 1,
      ""name"": ""Customer 1"",
      ""code"": ""C1"",
      ""contacts"": [
        {
          ""id"": 1,
          ""name"": ""Contact 1.1"",
          ""function"": ""CEO"",
          ""isAuthorizedToSign"": true,
          ""dateOfBirth"": ""1970-05-15""
        },
        {
          ""id"": 2,
          ""name"": ""Contact 1.2"",
          ""function"": ""Secretary"",
          ""isAuthorizedToSign"": false,
          ""dateOfBirth"": ""1980-06-16""
        },
        {
          ""id"": 3,
          ""name"": ""Contact 1.3"",
          ""function"": ""Sales Mgr"",
          ""isAuthorizedToSign"": false,
          ""dateOfBirth"": ""1990-07-17""
        }
      ]
    },
    {
      ""id"": 2,
      ""name"": ""Customer 2"",
      ""code"": ""C2"",
      ""contacts"": [
        {
          ""id"": 4,
          ""name"": ""Contact 2.1"",
          ""function"": ""CEO"",
          ""isAuthorizedToSign"": true,
          ""dateOfBirth"": ""1971-05-18""
        }
      ]
    },
    {
      ""id"": 3,
      ""name"": ""Customer 3"",
      ""code"": ""C3"",
      ""contacts"": []
    }
  ]
}"));

            Assert.That(result, Is.Not.Null);
            Assert.That(result, Has.Length.EqualTo(3));
        }

        [Test]
        public async Task TestGetSelection()
        {
            var result = await _client.Contacts
                .Select(x => new { x.Id, x.Name, DOB = x.DateOfBirth })
                .Where(x => x.Name.StartsWith("Contact 1"))
                .OrderByDescending(x => x.Id)
                .Skip(1)
                .ToArrayAsync();
            
            Assert.That(
                _client.Json, 
                Is.EqualTo(@"{
  ""query"": {
    ""select"": ""Id,Name,DateOfBirth"",
    ""filter"": ""startsWith(Name,'Contact 1')"",
    ""orderBy"": ""Id desc"",
    ""skip"": 1,
    ""take"": 200
  },
  ""data"": [
    {
      ""id"": 2,
      ""name"": ""Contact 1.2"",
      ""dateOfBirth"": ""1980-06-16""
    },
    {
      ""id"": 1,
      ""name"": ""Contact 1.1"",
      ""dateOfBirth"": ""1970-05-15""
    }
  ]
}"));

            Assert.That(result, Is.Not.Null);
            Assert.That(result, Has.Length.EqualTo(2));
        }

        [Test]
        public async Task TestGetSingleProperty()
        {
            var result = await _client.Contacts
                .Select(x => x.Id)
                .Where(x => x.Customer_Id != 2)
                .OrderByDescending(x => x.Name)
                .Skip(1)
                .ToArrayAsync();

            Assert.That(_client.Json, Is.EqualTo(@"{
  ""query"": {
    ""select"": ""Id"",
    ""filter"": ""Customer_Id ne 2"",
    ""orderBy"": ""Name desc"",
    ""skip"": 1,
    ""take"": 200
  },
  ""data"": [
    {
      ""id"": 2
    },
    {
      ""id"": 1
    }
  ]
}"));

            Assert.That(result, Is.Not.Null);
            Assert.That(result, Has.Length.EqualTo(2));
            Assert.That(result[0], Is.EqualTo(2));
            Assert.That(result[1], Is.EqualTo(1));
        }

        [Test]
        public async Task TestExtend()
        {
            var result = await _client.Contacts
                .Take(2)
                .ToArrayAsync();

            Assert.That(_client.Json, Is.EqualTo(@"{
  ""query"": {
    ""orderBy"": ""Id"",
    ""take"": 2
  },
  ""data"": [
    {
      ""id"": 1,
      ""name"": ""Contact 1.1"",
      ""function"": ""CEO"",
      ""isAuthorizedToSign"": true,
      ""dateOfBirth"": ""1970-05-15""
    },
    {
      ""id"": 2,
      ""name"": ""Contact 1.2"",
      ""function"": ""Secretary"",
      ""isAuthorizedToSign"": false,
      ""dateOfBirth"": ""1980-06-16""
    }
  ]
}"));

            Assert.That(_client.RequestUrl, Is.EqualTo("/Contact?take=2"));
            Assert.That(result[0].Customer, Is.Null);

            _client.QueryStringOverride = "extend=Customer(Id,Name)&take=2"; // Use query string override to avoid client requests excluded Customer.Code column
            result = await _client.Contacts
                .ToArrayAsync();

            Assert.That(_client.Json, Is.EqualTo(@"{
  ""query"": {
    ""extend"": ""Customer(Id,Name)"",
    ""orderBy"": ""Id"",
    ""take"": 2
  },
  ""data"": [
    {
      ""id"": 1,
      ""name"": ""Contact 1.1"",
      ""function"": ""CEO"",
      ""isAuthorizedToSign"": true,
      ""dateOfBirth"": ""1970-05-15"",
      ""customer"": {
        ""id"": 1,
        ""name"": ""Customer 1""
      }
    },
    {
      ""id"": 2,
      ""name"": ""Contact 1.2"",
      ""function"": ""Secretary"",
      ""isAuthorizedToSign"": false,
      ""dateOfBirth"": ""1980-06-16"",
      ""customer"": {
        ""id"": 1,
        ""name"": ""Customer 1""
      }
    }
  ]
}"));

            Assert.That(_client.RequestUrl, Is.EqualTo("/Contact?extend=Customer(Id,Name)&take=2"));
            Assert.That(result[0].Customer, Is.Not.Null);
        }

        [Test]
        public async Task TestRestrictedModel()
        {
            await _client.RestrictedCustomers.Where(x => x.Id == 1).ToArrayAsync();

            Assert.That(_client.RequestUrl, Is.EqualTo("/Customer?select=Id,Name&filter=Id eq 1"));
        }

        [Test]
        public async Task TestNotIndexedError()
        {
            Exception e = null;
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

        [Test]
        public async Task TestMaxPageSize()
        {
            var customers = await _client.Customers.Take(3).ToArrayAsync();
            Assert.That(customers, Has.Length.EqualTo(3));

            GraphqlConfiguration.DefaultMaxPageSize = 1;
            customers = await _client.Customers.ToArrayAsync();
            Assert.That(customers, Has.Length.EqualTo(1));

            GraphqlConfiguration.Configure<Customer>().MaxPageSize(2);
            customers = await _client.Customers.ToArrayAsync();
            Assert.That(customers, Has.Length.EqualTo(2));

            customers = await _client.Customers.Take(3).ToArrayAsync();
            Assert.That(customers, Has.Length.EqualTo(2));

            GraphqlConfiguration.DefaultMaxPageSize = 0;
            GraphqlConfiguration.Configure<Customer>().MaxPageSize(0);
        }

        [Test]
        public async Task TestCount()
        {
            var nrOfCustomers = await _client.Customers.CountAsync();
            Assert.That(+nrOfCustomers, Is.EqualTo(3));

            nrOfCustomers = await _client.Customers
                .Where(x => x.Id == 1)
                .CountAsync();
            Assert.That(+nrOfCustomers, Is.EqualTo(1));
        }

        [Test]
        public async Task TestManualUrls()
        {
            // Lowercase field names in the query should yield correctly cased field names in the response
            _client.QueryStringOverride = "select=id,name,dateofbirth,customer.name&take=1";
            var result = await _client.Contacts.Select(x => new { x.Id, x.Name, x.DateOfBirth, CustomerName = x.Customer.Name }).Take(1).ToArrayAsync();

            Assert.That(_client.Json, Is.EqualTo(@"{
  ""query"": {
    ""select"": ""id,name,dateofbirth,customer.name"",
    ""orderBy"": ""Id"",
    ""take"": 1
  },
  ""data"": [
    {
      ""id"": 1,
      ""name"": ""Contact 1.1"",
      ""dateOfBirth"": ""1970-05-15"",
      ""customer.Name"": ""Customer 1""
    }
  ]
}"));

            Assert.That(result, Is.Not.Null);
            Assert.That(result, Has.Length.EqualTo(1));
            Assert.That(result[0].Id, Is.EqualTo(1));
            Assert.That(result[0].Name, Is.EqualTo("Contact 1.1"));
            Assert.That(result[0].CustomerName, Is.EqualTo("Customer 1"));

            // Spaces in field names
            _client.QueryStringOverride = "select=id, name ,dateofbirth, customer.name&take=1&orderby= name &filter= id eq 1";
            result = await _client.Contacts.Select(x => new { x.Id, x.Name, x.DateOfBirth, CustomerName = x.Customer.Name }).Take(1).ToArrayAsync();

            Assert.That(result, Is.Not.Null);
            Assert.That(result, Has.Length.EqualTo(1));
            Assert.That(result[0].Id, Is.EqualTo(1));
            Assert.That(result[0].Name, Is.EqualTo("Contact 1.1"));
        }

        private class TestWebApp : WebApplicationFactory<Web.Core.Program>
        { }

        private class TestClient : ClientBase
        {
            private readonly HttpClient _httpClient;

            public string QueryStringOverride { get; set; }

            public string RequestUrl { get; private set; }

            public string Json { get; private set; }

            public TestClient(HttpClient httpClient)
            {
                Configuration.Url = string.Empty;
                Configuration.CreateHttpRequestMessageHandler = CreateRequest;
                Configuration.ProcessRequestHandler = ProcessRequest;
                _httpClient = httpClient;
            }

            private Task<HttpRequestMessage> CreateRequest(Configuration configuration, string entityName, HttpMethod httpMethod, string query, string payload)
            {
                if (QueryStringOverride != null)
                {
                    query = QueryStringOverride;
                    QueryStringOverride = null;
                }

                var request = new HttpRequestMessage(
                    httpMethod,
                    $"{configuration.Url}/{entityName}?{query}");
                if (!string.IsNullOrEmpty(configuration.Key))
                    request.Headers.Add(configuration.KeyHeaderName, configuration.Key);
                if (payload != null)
                    request.Content = new StringContent(payload);
                return Task.FromResult(request);
            }

            private async Task<string> ProcessRequest(Configuration configuration, HttpRequestMessage httpRequestMessage)
            {
                RequestUrl = httpRequestMessage.RequestUri.OriginalString;

                var response = await _httpClient.SendAsync(httpRequestMessage);
                if (!response.IsSuccessStatusCode)
                    throw new HttpStatusCodeException(response.StatusCode, response.ReasonPhrase, await response.Content.ReadAsStringAsync());

                Json = await response.Content.ReadAsStringAsync();

                return Json;
            }

            public ClientQuery<Customer> Customers => CreateQuery<Customer>();

            public ClientQuery<Contact> Contacts => CreateQuery<Contact>();

            public ClientQuery<RestrictedCustomer> RestrictedCustomers => CreateQuery<RestrictedCustomer>(true, "Customer");
        }

        private class RestrictedCustomer
        {
            public int Id { get; set; }
            public string Name { get; set; }
        }
    }
}
