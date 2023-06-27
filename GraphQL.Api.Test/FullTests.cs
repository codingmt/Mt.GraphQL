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

        [SetUp]
        public void Setup()
        {
        }

        [Test]
        public void TestAnonymousClass()
        {
            var clientQuery = new Query<Entity>()
                .Select(x => new { x.Id, EntityName = x.Name, RelatedName = x.Related.Name })
                .Where(x => x.Description.Contains("Entity"))
                .Skip(1)
                .Take(2);
            Assert.That(clientQuery.ToString(), Is.EqualTo("select=Id,Name,Related.Name&filter=contains(Description,'Entity')&skip=1&take=2"));

            var serverQuery = new Query<Entity>
            {
                Filter = clientQuery.Filter,
                Select = clientQuery.Select,
                Skip = clientQuery.Skip,
                Take = clientQuery.Take
            };
            var serverResult = _set.Apply(serverQuery);

            var json = serverResult.ToJson();
            Assert.That(json, Is.EqualTo(@"[
  {
    ""Id"": 2,
    ""Name"": ""B"",
    ""Related_Name"": ""Related to B""
  },
  {
    ""Id"": 5,
    ""Name"": ""E"",
    ""Related_Name"": ""Related to E""
  }
]"));

            var result = clientQuery.ParseJson(json);

            Assert.Multiple(() =>
            {
                Assert.That(result, Has.Length.EqualTo(2));
                Assert.That(result[0].Id, Is.EqualTo(2));
                Assert.That(result[0].EntityName, Is.EqualTo("B"));
                Assert.That(result[0].RelatedName, Is.EqualTo("Related to B"));
                Assert.That(result[1].Id, Is.EqualTo(5));
                Assert.That(result[1].EntityName, Is.EqualTo("E"));
                Assert.That(result[1].RelatedName, Is.EqualTo("Related to E"));
            });
        }

        [Test]
        public void TestSingleMemberLiteral()
        {
            var clientQuery = new Query<Entity>()
                .Select(x => x.Id)
                .Where(x => x.Description.Contains("Entiteit"))
                .Take(1);
            Assert.That(clientQuery.ToString(), Is.EqualTo("select=Id&filter=contains(Description,'Entiteit')&take=1"));

            var serverQuery = new Query<Entity>
            {
                Filter = clientQuery.Filter,
                Select = clientQuery.Select,
                Skip = clientQuery.Skip,
                Take = clientQuery.Take
            };
            var serverResult = _set.Apply(serverQuery);

            var json = serverResult.ToJson();
            Assert.That(json, Is.EqualTo(@"[
  3
]"));

            var result = clientQuery.ParseJson(json);
            Assert.Multiple(() =>
            {
                Assert.That(result, Has.Length.EqualTo(1));
                Assert.That(result[0], Is.EqualTo(3));
            });
        }

        [Test]
        public void TestSingleMemberObject()
        {
            var clientQuery = new Query<Entity>()
                .Select(x => x.Related)
                .Where(x => x.Description.Contains("Entiteit"));
            Assert.That(clientQuery.ToString(), Is.EqualTo("select=Related&filter=contains(Description,'Entiteit')"));

            var serverQuery = new Query<Entity>
            {
                Filter = clientQuery.Filter,
                Select = clientQuery.Select,
                Skip = clientQuery.Skip,
                Take = clientQuery.Take
            };
            var serverResult = _set.Apply(serverQuery);

            var json = serverResult.ToJson();
            Assert.That(json, Is.EqualTo(@"[
  {
    ""Id"": 0,
    ""Name"": ""Related to C"",
    ""Description"": null,
    ""Type"": null,
    ""IsCustomer"": false,
    ""Date"": null,
    ""Related"": null
  },
  null
]"));

            var result = clientQuery.ParseJson(json);
            Assert.Multiple(() =>
            {
                Assert.That(result, Has.Length.EqualTo(2));
                Assert.That(result[0].Name, Is.EqualTo("Related to C"));
                Assert.That(result[1], Is.Null);
            });
        }
    }
}