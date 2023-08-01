using System.Web;

namespace Mt.GraphQL.Api.Test
{
    public class FullTests
    {
        private static readonly Entity[] _set = new[]
        {
            new Entity { Id = 1, Name= "A", Description = "Entity A", Related = new Entity{ Name = "Related to A" } },
            new Entity { Id = 2, Name= "B", Description = "Entity B", Related = new Entity{ Name = "Related to B" } },
            new Entity { Id = 3, Name= "C", Description = "Entiteit C", Related = new Entity{ Name = "Related to C" } },
            new Entity { Id = 4, Name= "D", Description = "Entiteit D" },
            new Entity { Id = 5, Name= "E", Description = "Entity E", Related = new Entity{ Name = "Related to E" } },
            new Entity { Id = 6, Name= "F", Description = "Entity F", Related = new Entity{ Name = "Related to F" } }
        };

        private TestClient _client;

        [SetUp]
        public void Setup()
        {
            _client = new TestClient();
        }

        [Test]
        public async Task TestAnonymousClass()
        {
            var clientQuery = _client.Set
                .Select(x => new { x.Id, EntityName = x.Name, RelatedName = x.Related.Name })
                .Where(x => x.Description.Contains("Entity"))
                .OrderByDescending(x => x.Id)
                .OrderBy(x => x.Name)
                .Skip(1)
                .Take(2);
            Assert.That(clientQuery.ToString(), Is.EqualTo("select=Id,Name,Related.Name&filter=contains(Description,'Entity')&orderBy=Id desc,Name&skip=1&take=2"));

            var result = await clientQuery.ToArrayAsync();

            Assert.That(_client.Json, Is.EqualTo(@"[
  {
    ""Id"": 5,
    ""Name"": ""E"",
    ""Related.Name"": ""Related to E""
  },
  {
    ""Id"": 2,
    ""Name"": ""B"",
    ""Related.Name"": ""Related to B""
  }
]"));

            Assert.Multiple(() =>
            {
                Assert.That(result, Has.Length.EqualTo(2));
                Assert.That(result[0].Id, Is.EqualTo(5));
                Assert.That(result[0].EntityName, Is.EqualTo("E"));
                Assert.That(result[0].RelatedName, Is.EqualTo("Related to E"));
                Assert.That(result[1].Id, Is.EqualTo(2));
                Assert.That(result[1].EntityName, Is.EqualTo("B"));
                Assert.That(result[1].RelatedName, Is.EqualTo("Related to B"));
            });
        }

        [Test]
        public async Task TestSingleMemberLiteral()
        {
            var clientQuery = _client.Set
                .Select(x => x.Id)
                .Where(x => x.Description.Contains("Entiteit"))
                .Take(1);
            Assert.That(clientQuery.ToString(), Is.EqualTo("select=Id&filter=contains(Description,'Entiteit')&take=1"));

            var result = await clientQuery.ToArrayAsync();

            Assert.That(_client.Json, Is.EqualTo(@"[
  3
]"));

            Assert.Multiple(() =>
            {
                Assert.That(result, Is.Not.Null);
                Assert.That(result, Has.Length.EqualTo(1));
                Assert.That(result[0], Is.EqualTo(3));
            });
        }

        [Test]
        public async Task TestSingleMemberObject()
        {
            var clientQuery = _client.Set
                .Select(x => x.Related)
                .Where(x => x.Description.Contains("Entiteit"));
            Assert.That(clientQuery.ToString(), Is.EqualTo("select=Related&filter=contains(Description,'Entiteit')"));

            var result = await clientQuery.ToArrayAsync();

            Assert.That(_client.Json, Is.EqualTo(@"[
  {
    ""Id"": 0,
    ""Name"": ""Related to C"",
    ""Description"": null,
    ""Type"": null,
    ""IsCustomer"": false,
    ""Date"": null,
    ""Related_Id"": null,
    ""Related"": null
  },
  null
]"));

            Assert.Multiple(() =>
            {
                Assert.That(result, Is.Not.Null);
                Assert.That(result, Has.Length.EqualTo(2));
                Assert.That(result[0].Name, Is.EqualTo("Related to C"));
                Assert.That(result[1], Is.Null);
            });
        }

        private class TestClient : ClientBase
        {
            public string? Json { get; private set; }

            public TestClient()
            {
                Configuration.Url = "https://localhost";
                Configuration.ProcessRequestHandler = ProcessRequest;
            }

            private Task<string> ProcessRequest(Configuration configuration, HttpRequestMessage httpRequestMessage)
            {
                var parameters = HttpUtility.ParseQueryString(httpRequestMessage.RequestUri?.Query ?? throw new Exception("No URI or querystring."));
                var query = new Query<Entity>
                {
                    Filter = parameters["filter"],
                    Select = parameters["select"],
                    OrderBy = parameters["orderBy"],
                    Skip = parameters["skip"].CastIfNotNull<int>(),
                    Take = parameters["take"].CastIfNotNull<int>()
                };
                Json = _set.Apply(query).ToJson();
                return Task.FromResult(Json);
            }

            public ClientQuery<Entity> Set => CreateQuery<Entity>();
        }
    }
}