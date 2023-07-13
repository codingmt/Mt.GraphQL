using Mt.GraphQL.Api;
using Mt.GraphQL.Internal;
using System;
using System.Linq;
using System.Reflection;

namespace System.Collections.Generic
{
    /// <summary>
    /// Extensions for <see cref="IEnumerable"/>.
    /// </summary>
    public static class EnumerableExtensions
    {
        private static readonly MethodInfo _selectMethod = typeof(Enumerable).GetMethods().Single(m =>
            m.Name == nameof(Enumerable.Select) &&
            m.GetParameters().Select(p => p.ParameterType.GetGenericTypeDefinition()).SequenceEqual(
            new[] { typeof(IEnumerable<>), typeof(Func<,>) }))
            ?? throw new Exception("Method Select not found.");
        private static readonly MethodInfo _whereMethod = typeof(Enumerable).GetMethods().Single(m =>
            m.Name == nameof(Enumerable.Where) &&
            m.GetParameters().Select(p => p.ParameterType.GetGenericTypeDefinition()).SequenceEqual(
            new[] { typeof(IEnumerable<>), typeof(Func<,>) }))
            ?? throw new Exception("Method Where not found.");
        private static readonly MethodInfo _orderByMethod = typeof(Enumerable).GetMethods().Single(m =>
            m.Name == nameof(Enumerable.OrderBy) &&
            m.GetParameters().Length == 2)
            ?? throw new Exception("Method OrderBy not found.");
        private static readonly MethodInfo _thenByMethod = typeof(Enumerable).GetMethods().Single(m =>
            m.Name == nameof(Enumerable.ThenBy) &&
            m.GetParameters().Length == 2)
            ?? throw new Exception("Method ThenBy not found.");
        private static readonly MethodInfo _orderByDescendingMethod = typeof(Enumerable).GetMethods().Single(m =>
            m.Name == nameof(Enumerable.OrderByDescending) &&
            m.GetParameters().Length == 2)
            ?? throw new Exception("Method OrderByDescending not found.");
        private static readonly MethodInfo _thenByDescendingMethod = typeof(Enumerable).GetMethods().Single(m =>
            m.Name == nameof(Enumerable.ThenByDescending) &&
            m.GetParameters().Length == 2)
            ?? throw new Exception("Method ThenByDescending not found.");
        private static readonly MethodInfo _skipMethod = typeof(Enumerable).GetMethods().First(m =>
            m.Name == nameof(Enumerable.Skip) && m.IsPublic)
            ?? throw new Exception("Method Skip not found.");
        private static readonly MethodInfo _takeMethod = typeof(Enumerable).GetMethods().First(m =>
            m.Name == nameof(Enumerable.Take) && m.IsPublic)
            ?? throw new Exception("Method Take not found.");

        /// <summary>
        /// Apply the <paramref name="query"/> to the <paramref name="source"/>.
        /// </summary>
        /// <typeparam name="T">The type of <see cref="IEnumerable{T}"/> <paramref name="source"/>.</typeparam>
        /// <typeparam name="TResult">The type of <see cref="IEnumerable{T}"/> result.</typeparam>
        /// <param name="source">The source to apply the <paramref name="query"/> to.</param>
        /// <param name="query">The query to apply to the <paramref name="source"/>.</param>
        public static IEnumerable<TResult> Apply<T, TResult>(this IEnumerable<T> source, Query<T, TResult> query)
            where T : class =>
            (IEnumerable<TResult>)InnerApply(source, query);

        /// <summary>
        /// Apply the <paramref name="query"/> to the <paramref name="source"/>.
        /// </summary>
        /// <typeparam name="T">The type of <see cref="IEnumerable{T}"/> <paramref name="source"/>.</typeparam>
        /// <param name="source">The source to apply the <paramref name="query"/> to.</param>
        /// <param name="query">The query to apply to the <paramref name="source"/>.</param>
        public static IEnumerable Apply<T>(this IEnumerable<T> source, Query<T> query)
            where T : class => 
            InnerApply(source, query);

        private static IEnumerable InnerApply<T>(IEnumerable<T> source, IQueryInternal<T> query)
            where T : class
        {
            IEnumerable result = source;

            if (query.Expressions.FilterExpression != null)
            {
                var whereMethod = _whereMethod.MakeGenericMethod(typeof(T));
                result = (IEnumerable)whereMethod.Invoke(null, new object[] { result, query.Expressions.FilterExpression.Compile() });
            }

            var i = 0;
            foreach (var (member, descending) in query.Expressions.OrderBy) 
            {
                var orderMethodInfo = i == 0
                    ? (descending ? _orderByDescendingMethod : _orderByMethod)
                    : (descending ? _thenByDescendingMethod : _thenByMethod);
                var orderMethod = orderMethodInfo.MakeGenericMethod(typeof(T), member.ReturnType);
                result = (IEnumerable)orderMethod.Invoke(null, new object[] { result, member.Compile() });
                i++;
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
