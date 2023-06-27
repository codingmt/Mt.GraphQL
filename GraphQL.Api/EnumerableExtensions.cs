using Mt.GraphQL.Api;
using Newtonsoft.Json.Linq;
using System;
using System.Linq;
using System.Reflection;

namespace System.Collections.Generic
{
    public static class EnumerableExtensions
    {
        private static readonly MethodInfo _selectMethod = typeof(Enumerable).GetMethods().Single(m =>
            m.Name == nameof(Enumerable.Select) &&
            m.GetParameters().Select(p => p.ParameterType.GetGenericTypeDefinition()).SequenceEqual(
            new[] { typeof(IEnumerable<>), typeof(Func<,>) }));
        private static readonly MethodInfo _whereMethod = typeof(Enumerable).GetMethods().Single(m =>
            m.Name == nameof(Enumerable.Where) &&
            m.GetParameters().Select(p => p.ParameterType.GetGenericTypeDefinition()).SequenceEqual(
            new[] { typeof(IEnumerable<>), typeof(Func<,>) }));
        private static readonly MethodInfo _skipMethod = typeof(Enumerable).GetMethods().First(m =>
            m.Name == nameof(Enumerable.Skip) && m.IsPublic);
        private static readonly MethodInfo _takeMethod = typeof(Enumerable).GetMethods().First(m =>
            m.Name == nameof(Enumerable.Take) && m.IsPublic);

        public static IEnumerable Apply<T>(this IEnumerable<T> source, Query<T> query)
            where T : class => 
            InnerApply(source, query);

        public static IEnumerable<TResult> Apply<T, TResult>(this IEnumerable<T> source, Query<T, TResult> query)
            where T : class =>
            (IEnumerable<TResult>)InnerApply(source, query);

        public static string ToJson(this IEnumerable source) =>
            JArray.FromObject(source).ToString();

        private static IEnumerable InnerApply<T>(IEnumerable<T> source, Query<T> query)
            where T : class
        {
            IEnumerable result = source;

            if (query.Expressions.FilterExpression != null)
            {
                var whereMethod = _whereMethod.MakeGenericMethod(typeof(T));
                result = (IEnumerable)whereMethod.Invoke(null, new object[] { result, query.Expressions.FilterExpression.Compile() });
            }

            if (query.Skip.HasValue)
            {
                var skipMethod = _skipMethod.MakeGenericMethod(typeof(T));
                result = (IEnumerable)skipMethod.Invoke(null, new object[] { result, query.Skip.Value });
            }

            if (query.Take.HasValue)
            {
                var takeMethod = _takeMethod.MakeGenericMethod(typeof(T));
                result = (IEnumerable)takeMethod.Invoke(null, new object[] { result, query.Take.Value });
            }

            if (query.Expressions.SelectExpression != null)
            {
                var selectMethod = _selectMethod.MakeGenericMethod(typeof(T), query.Expressions.SelectExpression.ReturnType);
                result = (IEnumerable)selectMethod.Invoke(null, new object[] { result, query.Expressions.SelectExpression.Compile() });
            }

            return result;
        }
    }
}
