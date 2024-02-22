using Mt.GraphQL.Api;
using Mt.GraphQL.Internal;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace System.Collections.Generic
{
    /// <summary>
    /// Extensions for <see cref="IEnumerable"/>.
    /// </summary>
    public static class EnumerableExtensions
    {
        private static readonly MethodInfo _selectMethod = GetMethodInfo(e => e.Select(x => x));
        private static readonly MethodInfo _whereMethod = GetMethodInfo(e => e.Where(x => true));
        private static readonly MethodInfo _orderByMethod = GetMethodInfo(e => e.OrderBy(x => 1));
        private static readonly MethodInfo _thenByMethod = GetMethodInfo(e => e.OrderBy(x => 1).ThenBy(x => 1));
        private static readonly MethodInfo _orderByDescendingMethod = GetMethodInfo(e => e.OrderByDescending(x => 1));
        private static readonly MethodInfo _thenByDescendingMethod = GetMethodInfo(e => e.OrderBy(x => 1).ThenByDescending(x => 1));
        private static readonly MethodInfo _skipMethod = GetMethodInfo(x => x.Skip(0));
        private static readonly MethodInfo _takeMethod = GetMethodInfo(x => x.Take(0));
        private static readonly MethodInfo _countMethod = GetMethodInfo(x => x.Count());

        private static MethodInfo GetMethodInfo(Expression<Func<IEnumerable<object>, IEnumerable<object>>> method) =>
            (method.Body as MethodCallExpression).Method.GetGenericMethodDefinition();
        private static MethodInfo GetMethodInfo<TResult>(Expression<Func<IEnumerable<object>, TResult>> method) =>
            (method.Body as MethodCallExpression).Method.GetGenericMethodDefinition();

        /// <summary>
        /// Apply the <paramref name="query"/> to the <paramref name="source"/>.
        /// </summary>
        /// <typeparam name="T">The type of <see cref="IEnumerable{T}"/> <paramref name="source"/>.</typeparam>
        /// <typeparam name="TResult">The type of <see cref="IEnumerable{T}"/> result.</typeparam>
        /// <param name="source">The source to apply the <paramref name="query"/> to.</param>
        /// <param name="query">The query to apply to the <paramref name="source"/>.</param>
        public static object Apply<T, TResult>(this IEnumerable<T> source, Query<T, TResult> query)
            where T : class =>
            InnerApply(source, query);

        /// <summary>
        /// Apply the <paramref name="query"/> to the <paramref name="source"/>.
        /// </summary>
        /// <typeparam name="T">The type of <see cref="IEnumerable{T}"/> <paramref name="source"/>.</typeparam>
        /// <param name="source">The source to apply the <paramref name="query"/> to.</param>
        /// <param name="query">The query to apply to the <paramref name="source"/>.</param>
        public static object Apply<T>(this IEnumerable<T> source, Query<T> query)
            where T : class => 
            InnerApply(source, query);

        private static object InnerApply<T>(IEnumerable<T> source, IQueryInternal<T> query)
            where T : class
        {
            IEnumerable set = source;

            if (query.Expressions.FilterExpression != null)
            {
                var whereMethod = _whereMethod.MakeGenericMethod(typeof(T));
                set = (IEnumerable)whereMethod.Invoke(null, new object[] { set, query.Expressions.FilterExpression.Compile() });
            }

            if (query.Count == true)
            {
                var countMethod = _countMethod.MakeGenericMethod(typeof(T));
                return new QueryResponse<int>((IQuery)query, (int)countMethod.Invoke(null, new object[] { set }));
            }

            var isFirst = true;
            foreach (var (member, descending) in query.Expressions.OrderBy) 
            {
                var orderMethodInfo = isFirst
                    ? (descending ? _orderByDescendingMethod : _orderByMethod)
                    : (descending ? _thenByDescendingMethod : _thenByMethod);
                var orderMethod = orderMethodInfo.MakeGenericMethod(typeof(T), member.ReturnType);
                set = (IEnumerable)orderMethod.Invoke(null, new object[] { set, member.Compile() });
                isFirst = false;
            }

            if (query.Skip.HasValue)
            {
                var skipMethod = _skipMethod.MakeGenericMethod(typeof(T));
                set = (IEnumerable)skipMethod.Invoke(null, new object[] { set, query.Skip.Value });
            }

            var take = Mt.GraphQL.Internal.Configuration.GetTypeConfiguration<T>().GetPageSize(query.Take);
            if (take.HasValue)
            {
                query.Take = take;
                var takeMethod = _takeMethod.MakeGenericMethod(typeof(T));
                set = (IEnumerable)takeMethod.Invoke(null, new object[] { set, take });
            }

            var selectExpression = query.Expressions.GetActualSelectExpression(true);
            var selectMethod = _selectMethod.MakeGenericMethod(typeof(T), selectExpression.ReturnType);
            set = (IEnumerable)selectMethod.Invoke(null, new object[] { set, selectExpression.Compile() });

            var result = new List<object>();
            var enumerator = set.GetEnumerator();
            while (enumerator.MoveNext())
                result.Add(enumerator.Current);

            return new QueryResponse<object>((IQuery)query, result);
        }
    }
}
