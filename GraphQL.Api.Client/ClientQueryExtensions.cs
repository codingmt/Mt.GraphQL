using System.Linq.Expressions;
using System;

namespace Mt.GraphQL.Api
{
    /// <summary>
    /// Extensions to <see cref="ClientQuery{T}"/> and <see cref="ClientQuery{T, TResult}"/>.
    /// </summary>
    public static class ClientQueryExtensions
    {
        internal static ClientBase GetClient(this IClientQuery clientQuery) =>
            clientQuery.Client ?? throw new Exception("Client not set.");

        internal static void SetClient(this IClientQuery clientQuery, ClientBase client) =>
            clientQuery.Client = client;

        internal static void CopyClientFrom(this IClientQuery clientQuery, IClientQuery from) =>
            clientQuery.Client = from.Client ?? throw new Exception("Client not set.");

        /// <summary>
        /// Selects one or more fields from the source type <typeparamref name="T"/>, resulting in type <typeparamref name="TResult"/>.
        /// </summary>
        /// <typeparam name="T">The type of source entity.</typeparam>
        /// <typeparam name="TResult">The type of selection.</typeparam>
        /// <param name="query">The <see cref="Query{T}"/> to apply the selection to.</param>
        /// <param name="selection">A <see cref="LambdaExpression"/> selecting the field(s).</param>
        public static ClientQuery<T, TResult> Select<T, TResult>(this ClientQuery<T> query, Expression<Func<T, TResult>> selection)
            where T : class =>
            (ClientQuery<T, TResult>)QueryExtensions.Select(query, selection);

        /// <summary>
        /// Sets the filter on type <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="T">The type of source entity.</typeparam>
        /// <typeparam name="TResult">The type of selection.</typeparam>
        /// <param name="query">The <see cref="Query{T, TResult}"/> to apply the filter to.</param>
        /// <param name="condition">A <see cref="LambdaExpression"/> filtering the source.</param>
        public static ClientQuery<T, TResult> Where<T, TResult>(this ClientQuery<T, TResult> query, Expression<Func<T, bool>> condition)
            where T : class =>
            (ClientQuery<T, TResult>)QueryExtensions.Where(query, condition);

        /// <summary>
        /// Sets the filter on type <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="T">The type of source entity.</typeparam>
        /// <param name="query">The <see cref="Query{T}"/> to apply the filter to.</param>
        /// <param name="condition">A <see cref="LambdaExpression"/> filtering the source.</param>
        public static ClientQuery<T> Where<T>(this ClientQuery<T> query, Expression<Func<T, bool>> condition)
            where T : class =>
            (ClientQuery<T>)QueryExtensions.Where(query, condition);

        /// <summary>
        /// Orders the set ascending.
        /// </summary>
        /// <typeparam name="T">The type of source entity.</typeparam>
        /// <typeparam name="TResult">The type of selection.</typeparam>
        /// <typeparam name="TMember">The type of meber to order by.</typeparam>
        /// <param name="query">The <see cref="Query{T, TResult}"/> to apply the ordering to.</param>
        /// <param name="member">A <see cref="LambdaExpression"/> selecting the member to order by.</param>
        public static ClientQuery<T, TResult> OrderBy<T, TResult, TMember>(this ClientQuery<T, TResult> query, Expression<Func<T, TMember>> member)
            where T : class =>
            (ClientQuery<T, TResult>)QueryExtensions.OrderBy(query, member);

        /// <summary>
        /// Orders the set ascending.
        /// </summary>
        /// <typeparam name="T">The type of source entity.</typeparam>
        /// <typeparam name="TMember">The type of meber to order by.</typeparam>
        /// <param name="query">The <see cref="Query{T}"/> to apply the ordering to.</param>
        /// <param name="member">A <see cref="LambdaExpression"/> selecting the member to order by.</param>
        public static ClientQuery<T> OrderBy<T, TMember>(this ClientQuery<T> query, Expression<Func<T, TMember>> member)
            where T : class =>
            (ClientQuery<T>)QueryExtensions.OrderBy(query, member);

        /// <summary>
        /// Orders the set descending.
        /// </summary>
        /// <typeparam name="T">The type of source entity.</typeparam>
        /// <typeparam name="TResult">The type of selection.</typeparam>
        /// <typeparam name="TMember">The type of meber to order by.</typeparam>
        /// <param name="query">The <see cref="Query{T, TResult}"/> to apply the ordering to.</param>
        /// <param name="member">A <see cref="LambdaExpression"/> selecting the member to order by.</param>
        public static ClientQuery<T, TResult> OrderByDescending<T, TResult, TMember>(this ClientQuery<T, TResult> query, Expression<Func<T, TMember>> member)
            where T : class =>
            (ClientQuery<T, TResult>)QueryExtensions.OrderByDescending(query, member);

        /// <summary>
        /// Orders the set descending.
        /// </summary>
        /// <typeparam name="T">The type of source entity.</typeparam>
        /// <typeparam name="TMember">The type of meber to order by.</typeparam>
        /// <param name="query">The <see cref="Query{T}"/> to apply the ordering to.</param>
        /// <param name="member">A <see cref="LambdaExpression"/> selecting the member to order by.</param>
        public static ClientQuery<T> OrderByDescending<T, TMember>(this ClientQuery<T> query, Expression<Func<T, TMember>> member)
            where T : class =>
            (ClientQuery<T>)QueryExtensions.OrderByDescending(query, member);

        /// <summary>
        /// Skips <paramref name="skip"/> items.
        /// </summary>
        /// <typeparam name="T">The type of source entity.</typeparam>
        /// <typeparam name="TResult">The type of selection.</typeparam>
        /// <param name="query">The <see cref="Query{T, TResult}"/> to apply the number of skipped items to.</param>
        /// <param name="skip">The number of items to skip.</param>
        public static ClientQuery<T, TResult> Skip<T, TResult>(this ClientQuery<T, TResult> query, int skip)
            where T : class =>
            (ClientQuery<T, TResult>)QueryExtensions.Skip(query, skip);

        /// <summary>
        /// Skips <paramref name="skip"/> items.
        /// </summary>
        /// <typeparam name="T">The type of source entity.</typeparam>
        /// <param name="query">The <see cref="Query{T}"/> to apply the number of skipped items to.</param>
        /// <param name="skip">The number of items to skip.</param>
        public static ClientQuery<T> Skip<T>(this ClientQuery<T> query, int skip)
            where T : class =>
            (ClientQuery<T>)QueryExtensions.Skip(query, skip);

        /// <summary>
        /// Takes <paramref name="take"/> items.
        /// </summary>
        /// <typeparam name="T">The type of source entity.</typeparam>
        /// <typeparam name="TResult">The type of selection.</typeparam>
        /// <param name="query">The <see cref="Query{T, TResult}"/> to apply the number of items to take to.</param>
        /// <param name="take">The number of items to take.</param>
        public static ClientQuery<T, TResult> Take<T, TResult>(this ClientQuery<T, TResult> query, int take)
            where T : class =>
            (ClientQuery<T, TResult>)QueryExtensions.Take(query, take);

        /// <summary>
        /// Takes <paramref name="take"/> items.
        /// </summary>
        /// <typeparam name="T">The type of source entity.</typeparam>
        /// <param name="query">The <see cref="Query{T}"/> to apply the number of items to take to.</param>
        /// <param name="take">The number of items to take.</param>
        public static ClientQuery<T> Take<T>(this ClientQuery<T> query, int take)
            where T : class =>
            (ClientQuery<T>)QueryExtensions.Take(query, take);
    }
}
