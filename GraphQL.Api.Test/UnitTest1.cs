namespace Mt.GraphQL.Api.Test
{
    public class Tests
    {
        [SetUp]
        public void Setup()
        {
        }

        [Test]
        public void Test1()
        {
            var query = new Query<Entity>()
                .Select(x => new { x.Id, x.Name, RelatedName = x.Related.Name })
                .Where(x => x.Description.Contains("Entity"))
                .Skip(1)
                .Take(2);
            Assert.That(query.ToString(), Is.EqualTo("select=Id,Name,Related.Name&filter=contains(Description,'Entity')&skip=1&take=2"));

            var set = new[]
            {
                new Entity { Id = 1, Name= "A", Description = "Entity A", Related = new Entity{ Name = "Related to A" } },
                new Entity { Id = 2, Name= "B", Description = "Entity B", Related = new Entity{ Name = "Related to B" } },
                new Entity { Id = 3, Name= "C", Description = "Entiteit C", Related = new Entity{ Name = "Related to C" } },
                new Entity { Id = 4, Name= "D", Description = "Entiteit D", Related = new Entity{ Name = "Related to D" } },
                new Entity { Id = 5, Name= "E", Description = "Entity E", Related = new Entity{ Name = "Related to E" } },
                new Entity { Id = 6, Name= "F", Description = "Entity F", Related = new Entity{ Name = "Related to F" } }
            };
            var result = set.Apply(query).ToArray();

            Assert.Multiple(() =>
            {
                Assert.That(result, Has.Length.EqualTo(2));
                Assert.That(result[0].Id, Is.EqualTo(2));
                Assert.That(result[0].Name, Is.EqualTo("B"));
                Assert.That(result[0].RelatedName, Is.EqualTo("Related to B"));
                Assert.That(result[1].Id, Is.EqualTo(5));
                Assert.That(result[1].Name, Is.EqualTo("E"));
                Assert.That(result[1].RelatedName, Is.EqualTo("Related to E"));
            });
        }
    }
}