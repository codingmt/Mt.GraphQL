using System.Collections.Generic;

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
                .Where(x => x.Description.Contains("Entity"));
            Assert.That(query.ToString(), Is.EqualTo("select=Id,Name,Related.Name&filter=contains(Description,'Entity')"));

            var set = new[]
            {
                new Entity { Id = 1, Name= "A", Description = "Entity A", Related = new Entity{ Name = "Related to A" } },
                new Entity { Id = 2, Name= "B", Description = "Entity B", Related = new Entity{ Name = "Related to B" } },
                new Entity { Id = 3, Name= "C", Description = "Entiteit C", Related = new Entity{ Name = "Related to C" } },
                new Entity { Id = 4, Name= "D", Description = "Entiteit D", Related = new Entity{ Name = "Related to D" } }
            };
            var result = set.Apply(query).ToArray();

            Assert.That(result.Length == 2);
            Assert.That(result[0].Id == 1);
            Assert.That(result[0].Name == "A");
            Assert.That(result[0].RelatedName == "Related to A");
            Assert.That(result[1].Id == 2);
            Assert.That(result[1].Name == "B");
            Assert.That(result[1].RelatedName == "Related to B");
        }
    }
}