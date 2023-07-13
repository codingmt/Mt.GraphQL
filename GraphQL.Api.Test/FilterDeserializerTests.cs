using static Mt.GraphQL.Api.Test.TestHelpers;

namespace Mt.GraphQL.Api.Test
{
    public class FilterDeserializerTests
    {
        [Test]
        [TestCase("Id le 5", "x => (x.Id <= 5)")]
        [TestCase("Date gt '2023-03-29'", "x => (x.Date > Convert(29-3-2023 00:00:00, Nullable`1))")]
        [TestCase("Date ge '2023-03-29T07:08:09'", "x => (x.Date >= Convert(29-3-2023 07:08:09, Nullable`1))")]
        [TestCase("Name eq 'A'", "x => (x.Name == \"A\")")]
        [TestCase("startsWith(Description,'Ent''ity')", "x => x.Description.StartsWith(\"Ent'ity\", OrdinalIgnoreCase)")]
        [TestCase("not(IsCustomer)", "x => Not(x.IsCustomer)")]
        [TestCase("Id in (1, 2,3)", "x => new [] {1, 2, 3}.Contains(x.Id)")]
        [TestCase("Id in 1, 2,3", "x => new [] {1, 2, 3}.Contains(x.Id)")]
        public void TestSimpleComparisons(string filter, string expectedExpression)
        {
            Filter(filter).HasFilterExpression(expectedExpression);
        }

        [Test]
        [TestCase(
            "Id eq 3 or Id eq 4", 
            "x => ((x.Id == 3) OrElse (x.Id == 4))")]
        [TestCase(
            "Id ne 3 and contains(Description,'A') or endsWith(Description,'B')", 
            "x => (((x.Id != 3) AndAlso x.Description.Contains(\"A\", OrdinalIgnoreCase)) OrElse x.Description.EndsWith(\"B\", OrdinalIgnoreCase))")]
        [TestCase(
            "Id lt 3 and (contains(Description,'A') or endsWith(Description,'B'))", 
            "x => ((x.Id < 3) AndAlso (x.Description.Contains(\"A\", OrdinalIgnoreCase) OrElse x.Description.EndsWith(\"B\", OrdinalIgnoreCase)))")]
        [TestCase(
            "Id gt 3 and contains(Description,'A') or endsWith(Description,'B')", 
            "x => (((x.Id > 3) AndAlso x.Description.Contains(\"A\", OrdinalIgnoreCase)) OrElse x.Description.EndsWith(\"B\", OrdinalIgnoreCase))")]
        public void TestCompositeFilters(string filter, string expectedExpression)
        {
            Filter(filter).HasFilterExpression(expectedExpression);
        }

        [Test]
        [TestCase("Bla", "QueryParseException", "Property Bla was not found on type Entity")]
        [TestCase("Id is null", "QueryParseException", "Unknown operator is")]
        [TestCase("Id eq 'five'", "QueryParseException", "Could not parse Int32 constant with value: 'five'")]
        [TestCase("(Id eq 1", "QueryParseException", "Closing parenthesis not found")]
        [TestCase("Id eq 1)", "QueryParseException", "Could not parse filter from position 7")]
        [TestCase("Id in (1", "QueryParseException", "Expected closing parenthesis at position 8")]
        [TestCase("Id in 1)", "QueryParseException", "Could not parse filter from position 7")]
        public void TestFilterErrors(string filter, string expectedException, string expectedErrorMessage)
        {
            Filter(filter).Throws(expectedException, expectedErrorMessage);
        }
    }
}
