using System.Linq.Expressions;

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
#if DEBUG
            if (query.FilterExpression?.ToString() != expression)
                throw new Exception($"Mismatch!\nExpected: {expression}\nActually: {query.FilterExpression?.ToString()}");
#else
            throw new Exception($"{nameof(TestHelpers)}.{nameof(HasFilterExpression)} can only be used in DEBUG mode.");
#endif
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
    } 
}
