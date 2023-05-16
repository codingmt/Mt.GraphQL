using Mt.GraphQL.Api;
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

        public static IEnumerable Apply<T>(this IEnumerable<T> source, Query<T> query) => 
            InnerApply(source, query);

        public static IEnumerable<TResult> Apply<T, TResult>(this IEnumerable<T> source, Query<T, TResult> query) =>
            (IEnumerable<TResult>)InnerApply(source, query);

        private static IEnumerable InnerApply<T>(IEnumerable<T> source, Query<T> query)
        {
            IEnumerable result = source;

            if (query.Expressions.FilterExpression != null)
            {
                var whereMethod = _whereMethod.MakeGenericMethod(typeof(T));
                result = (IEnumerable)whereMethod.Invoke(null, new object[] { result, query.Expressions.FilterExpression.Compile() });
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
