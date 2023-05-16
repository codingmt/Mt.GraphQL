using System.Linq.Expressions;
using static Mt.GraphQL.Api.Test.TestHelpers;

namespace Mt.GraphQL.Api.Test
{
    public class FilterSerializerTests
    {
        private static readonly DateTime _dt = new(2023, 3, 29, 7, 8, 9);
        private static readonly int _id = 3;
        private static readonly int[] _ids = new[] { 1, 2 }; 

        #pragma warning disable CS8602 // Dereference of a possibly null reference.
        private static readonly Dictionary<string, Expression<Func<Entity, bool>>> _testExpressions =
            new()
            {
                { "SimpleLe", x => x.Id <= 5 },
                { "SimpleGtDate", x => x.Date > new DateTime(2023, 3, 29) },
                { "SimpleGeDateTime", x => x.Date >= _dt },
                { "SimpleEqString", x => x.Name == "A" },
                { "SimpleStartsWith", x => x.Description.StartsWith("Ent'ity") },
                { "SimpleNotBool", x => !x.IsCustomer },
                { "SimpleIn1", x => new []{ 1, 2, 3 }.Contains(x.Id) },
                { "SimpleIn2", x => _ids.Contains(x.Id) },

                { "CompositeOr", x => x.Id == _id || x.Id == 4 || _ids.Contains(x.Id) },
                { "CompositeAndOr", x => x.Id != _id && x.Description.Contains("A") || x.Description.EndsWith("B") },
                { "CompositeAndOrParentheses1", x => x.Id < _id && (x.Description.Contains("A") || x.Description.EndsWith("B")) },
                { "CompositeAndOrParentheses2", x => (x.Id > _id && x.Description.Contains("A")) || x.Description.EndsWith("B") }
            };
        #pragma warning restore CS8602 // Dereference of a possibly null reference.

        [Test]
        [TestCase("SimpleLe", "Id le 5")]
        [TestCase("SimpleGtDate", "Date gt '2023-03-29'")]
        [TestCase("SimpleGeDateTime", "Date ge '2023-03-29T07:08:09'")]
        [TestCase("SimpleEqString", "Name eq 'A'")]
        [TestCase("SimpleStartsWith", "startsWith(Description,'Ent''ity')")]
        [TestCase("SimpleNotBool", "not(IsCustomer)")]
        [TestCase("SimpleIn1", "Id in (1, 2, 3)")]
        [TestCase("SimpleIn2", "Id in (1, 2)")]
        public void TestSimpleComparisons(string expression, string expectedQuery)
        {
            Expression(_testExpressions[expression]).YieldsQuery(expectedQuery);
        }

        [Test]
        [TestCase("CompositeOr", "Id eq 3 or Id eq 4 or Id in (1, 2)")]
        [TestCase("CompositeAndOr", "Id ne 3 and contains(Description,'A') or endsWith(Description,'B')")]
        [TestCase("CompositeAndOrParentheses1", "Id lt 3 and (contains(Description,'A') or endsWith(Description,'B'))")]
        [TestCase("CompositeAndOrParentheses2", "Id gt 3 and contains(Description,'A') or endsWith(Description,'B')")]
        public void TestComposite(string expression, string expectedQuery)
        {
            Expression(_testExpressions[expression]).YieldsQuery(expectedQuery);
        }
    }
}
