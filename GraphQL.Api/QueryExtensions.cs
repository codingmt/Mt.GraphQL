using Newtonsoft.Json.Linq;
using System;
using System.Linq;
using System.Linq.Expressions;

namespace Mt.GraphQL.Api
{
    /// <summary>
    /// Extensions to <see cref="Query{T}"/> and <see cref="Query{T, TResult}"/>.
    /// </summary>
    public static class QueryExtensions
    {
        /// <summary>
        /// Selects one or more fields from the source type <typeparamref name="T"/>, resulting in type <typeparamref name="TResult"/>.
        /// </summary>
        /// <typeparam name="T">The type of source entity.</typeparam>
        /// <typeparam name="TResult">The type of selection.</typeparam>
        /// <param name="query">The <see cref="Query{T}"/> to apply the selection to.</param>
        /// <param name="selection">A <see cref="LambdaExpression"/> selecting the field(s).</param>
        public static Query<T, TResult> Select<T, TResult>(this Query<T> query, Expression<Func<T, TResult>> selection)
            where T : class
        {
            // Validate expression
            var validator = new SelectSerializer<T, TResult>(selection);

            // Create result
            var result = query.Clone<Query<T, TResult>>();
            result.Expressions.SelectExpression = selection;
            result.ResultMapping = validator.ResultMapping;
            return result;
        }

        /// <summary>
        /// Sets the filter on type <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="T">The type of source entity.</typeparam>
        /// <typeparam name="TResult">The type of selection.</typeparam>
        /// <param name="query">The <see cref="Query{T, TResult}"/> to apply the filter to.</param>
        /// <param name="condition">A <see cref="LambdaExpression"/> filtering the source.</param>
        public static Query<T, TResult> Where<T, TResult>(this Query<T, TResult> query, Expression<Func<T, bool>> condition)
            where T : class
        {
            // Validate expression
            new FilterSerializer<T>(condition);

            // Create result
            var result = query.Clone<Query<T, TResult>>();
            result.Expressions.FilterExpression = condition;
            return result;
        }

        /// <summary>
        /// Sets the filter on type <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="T">The type of source entity.</typeparam>
        /// <param name="query">The <see cref="Query{T}"/> to apply the filter to.</param>
        /// <param name="condition">A <see cref="LambdaExpression"/> filtering the source.</param>
        public static Query<T> Where<T>(this Query<T> query, Expression<Func<T, bool>> condition)
            where T : class
        {
            // Validate expression
            new FilterSerializer<T>(condition);

            // Create result
            var result = query.Clone<Query<T>>();
            result.Expressions.FilterExpression = condition;
            return result;
        }

        /// <summary>
        /// Skips <paramref name="skip"/> items.
        /// </summary>
        /// <typeparam name="T">The type of source entity.</typeparam>
        /// <typeparam name="TResult">The type of selection.</typeparam>
        /// <param name="query">The <see cref="Query{T, TResult}"/> to apply the number of skipped items to.</param>
        /// <param name="skip">The number of items to skip.</param>
        public static Query<T, TResult> Skip<T, TResult>(this Query<T, TResult> query, int skip)
            where T : class
        {
            var result = query.Clone<Query<T, TResult>>();
            result.Skip = skip;
            return result;
        }

        /// <summary>
        /// Skips <paramref name="skip"/> items.
        /// </summary>
        /// <typeparam name="T">The type of source entity.</typeparam>
        /// <param name="query">The <see cref="Query{T}"/> to apply the number of skipped items to.</param>
        /// <param name="skip">The number of items to skip.</param>
        public static Query<T> Skip<T>(this Query<T> query, int skip)
            where T : class
        {
            var result = query.Clone<Query<T>>();
            result.Skip = skip;
            return result;
        }

        /// <summary>
        /// Takes <paramref name="take"/> items.
        /// </summary>
        /// <typeparam name="T">The type of source entity.</typeparam>
        /// <typeparam name="TResult">The type of selection.</typeparam>
        /// <param name="query">The <see cref="Query{T, TResult}"/> to apply the number of items to take to.</param>
        /// <param name="take">The number of items to take.</param>
        public static Query<T, TResult> Take<T, TResult>(this Query<T, TResult> query, int take)
            where T : class
        {
            var result = query.Clone<Query<T, TResult>>();
            result.Take = take;
            return result;
        }

        /// <summary>
        /// Takes <paramref name="take"/> items.
        /// </summary>
        /// <typeparam name="T">The type of source entity.</typeparam>
        /// <param name="query">The <see cref="Query{T}"/> to apply the number of items to take to.</param>
        /// <param name="take">The number of items to take.</param>
        public static Query<T> Take<T>(this Query<T> query, int take)
            where T : class
        {
            var result = query.Clone<Query<T>>();
            result.Take = take;
            return result;
        }

        /// <summary>
        /// Parses a received json message to the <typeparamref name="TResult"/> type.
        /// </summary>
        /// <typeparam name="T">The type of source entity.</typeparam>
        /// <typeparam name="TResult">The type of selection.</typeparam>
        /// <param name="query">The <see cref="Query{T, TResult}"/> used to request the received json.</param>
        /// <param name="json">The received json.</param>
        /// <returns>An array of <typeparamref name="TResult"/>.</returns>
        public static TResult[]? ParseJson<T, TResult>(this Query<T, TResult> query, string json)
            where T : class
        {
            var resultMapping = query.ResultMapping;
            if (resultMapping == null)
            {
                var visitor = new SelectSerializer<T, TResult>(
                    query.Expressions?.SelectExpression 
                    ?? throw new Exception("No Select expression present"));
                resultMapping = visitor.ResultMapping;
            }

            return JArray.Parse(json)?.Select(resultMapping).ToArray();
        }
    }
}
