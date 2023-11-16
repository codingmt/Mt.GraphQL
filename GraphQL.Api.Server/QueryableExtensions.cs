using Mt.GraphQL.Api;
using Mt.GraphQL.Internal;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;

namespace System.Linq
{
    /// <summary>
    /// Extensions for <see cref="IQueryable{T}"/>.
    /// </summary>
    public static class QueryableExtensions
    {
        private static readonly MethodInfo _selectMethod = GetMethodInfo(q => q.Select(x => x));
        private static readonly MethodInfo _whereMethod = GetMethodInfo(q => q.Where(x => true));
        private static readonly MethodInfo _orderByMethod = GetMethodInfo(q => q.OrderBy(x => 1));
        private static readonly MethodInfo _thenByMethod = GetMethodInfo(q => q.OrderBy(x => 1).ThenBy(x => 1));
        private static readonly MethodInfo _orderByDescendingMethod = GetMethodInfo(q => q.OrderByDescending(x => 1));
        private static readonly MethodInfo _thenByDescendingMethod = GetMethodInfo(q => q.OrderBy(x => 1).ThenByDescending(x => 1));
        private static readonly MethodInfo _skipMethod = GetMethodInfo(q => q.Skip(0));
        private static readonly MethodInfo _takeMethod = GetMethodInfo(q => q.Take(0));

        private static MethodInfo GetMethodInfo(Expression<Func<IQueryable<object>, IQueryable<object>>> method) =>
            (method.Body as MethodCallExpression).Method.GetGenericMethodDefinition();

        /// <summary>
        /// Apply the <paramref name="query"/> to the <paramref name="source"/>.
        /// </summary>
        /// <typeparam name="T">The type of <see cref="IQueryable{T}"/> <paramref name="source"/>.</typeparam>
        /// <typeparam name="TResult">The type of <see cref="IQueryable{T}"/> result.</typeparam>
        /// <param name="source">The source to apply the <paramref name="query"/> to.</param>
        /// <param name="query">The query to apply to the <paramref name="source"/>.</param>
        public static IQueryable<TResult> Apply<T, TResult>(this IQueryable<T> source, Query<T, TResult> query)
            where T : class =>
            (IQueryable<TResult>)InnerApply(source, query);

        /// <summary>
        /// Apply the <paramref name="query"/> to the <paramref name="source"/>.
        /// </summary>
        /// <typeparam name="T">The type of <see cref="IEnumerable{T}"/> <paramref name="source"/>.</typeparam>
        /// <param name="source">The source to apply the <paramref name="query"/> to.</param>
        /// <param name="query">The query to apply to the <paramref name="source"/>.</param>
        public static IQueryable Apply<T>(this IQueryable<T> source, Query<T> query)
            where T : class =>
            InnerApply(source, query);

        private static IQueryable InnerApply<T>(IQueryable<T> source, IQueryInternal<T> query)
            where T : class
        {
            if ((query.Take > 0 || query.Skip > 0) && !query.Expressions.OrderBy.Any())
                throw new QueryException(query.ToString(), "You cannot use Skip or Take without OrderBy or OrderByDescending.");

            IQueryable result = source;

            if (query.Expressions.FilterExpression != null)
            {
                var whereMethod = _whereMethod.MakeGenericMethod(typeof(T));
                result = (IQueryable)whereMethod.Invoke(null, new object[] { result, query.Expressions.FilterExpression });
            }

            var i = 0;
            foreach (var (member, descending) in query.Expressions.OrderBy)
            {
                var orderMethodInfo = i == 0
                    ? (descending ? _orderByDescendingMethod : _orderByMethod)
                    : (descending ? _thenByDescendingMethod : _thenByMethod);
                var orderMethod = orderMethodInfo.MakeGenericMethod(typeof(T), member.ReturnType);
                result = (IQueryable)orderMethod.Invoke(null, new object[] { result, member });
                i++;
            }

            if (query.Skip.HasValue)
            {
                var skipMethod = _skipMethod.MakeGenericMethod(typeof(T));
                result = (IQueryable)skipMethod.Invoke(null, new object[] { result, query.Skip.Value });
            }

            if (query.Take.HasValue)
            {
                var takeMethod = _takeMethod.MakeGenericMethod(typeof(T));
                result = (IQueryable)takeMethod.Invoke(null, new object[] { result, query.Take.Value });
            }

            var selectExpression = query.Expressions.GetActualSelectExpression();
            var selectMethod = _selectMethod.MakeGenericMethod(typeof(T), selectExpression.ReturnType);
            result = (IQueryable)selectMethod.Invoke(null, new object[] { result, selectExpression });

            return result;
        }

        /// <summary>
        /// Converts an <see cref="IQueryable"/> to an array of objects.
        /// </summary>
        /// <param name="query">The query to convert.</param>
        public static object[] ToArray(this IQueryable query)
        {
            var result = new List<object>();
            var enumerator = query.GetEnumerator();
            while (enumerator.MoveNext())
                result.Add(enumerator.Current);
            return result.ToArray();
        }
    }
}
