using Mt.GraphQL.Internal;
using System.Linq.Expressions;
using System.Text.Json;

namespace Mt.GraphQL.Api.Test
{
    internal static class TestHelpers
    {
        public static Func<Query<Entity>> Expression(Expression<Func<Entity, bool>> expression) =>
            () => new Query<Entity>().Where(expression);

        public static void YieldsQuery(this Func<Query<Entity>> entityQueryCreator, string query)
        {
            var entityQuery = entityQueryCreator();
            if (entityQuery.Filter != query)
                throw new Exception($"Mismatch!\nExpected: {query}\nActually: {entityQuery}");
        }

        public static Func<Query<Entity>> Filter(string filter) =>
            () => new() { Filter = filter };

        public static void HasFilterExpression(this Func<Query<Entity>> queryCreator, string expression)
        {
            var query = queryCreator();
            var filterExpression = query.AsQueryInternal().Expressions.FilterExpression;
            if (filterExpression?.ToString() != expression)
                throw new Exception($"Mismatch!\nExpected: {expression}\nActually: {filterExpression?.ToString()}");
        }

        public static void Throws(this Func<Query<Entity>> queryCreator, string exceptionType = null, string message = null)
        {
            try
            {
                queryCreator();
            }
            catch (Exception ex)
            {
                if (exceptionType != null && exceptionType != ex.GetType().Name)
                    throw new Exception($"Exception type mismatch:\nExpected: {exceptionType}\nActually: {ex.GetType().Name}");
                if (message != null && message != ex.Message)
                    throw new Exception($"Exception message mismatch:\nExpected: {message}\nActually: {ex.Message}");
                return;
            }

            throw new Exception($"Expected exception but the query did not throw.");
        }

        public static string ToJson(this object source) =>
            JsonSerializer.Serialize(
                source, 
                new JsonSerializerOptions 
                { 
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
                    WriteIndented = true 
                });

        public static T? CastIfNotNull<T>(this string value)
            where T: struct =>
            value == null ? null : (T)Convert.ChangeType(value, typeof(T));
    }
}
