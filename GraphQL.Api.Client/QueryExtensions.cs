using Mt.GraphQL.Internal;
using System;
using System.Linq;
using System.Linq.Expressions;
using System.Text.Json;
using System.Text.Json.Nodes;

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
            var result = query.AsQueryInternal().Clone<Query<T, TResult>>();
            result.AsQueryInternal().Expressions.SelectClause = new ExpressionSelectClause<T, TResult>(selection);
            result.AsQueryInternal().ResultMapping = validator.ResultMapping;
            return result;
        }

        /// <summary>
        /// Adds an extension to the query, including a nested entity in the model that would not be included by default.
        /// </summary>
        /// <typeparam name="T">The type that contains the extension.</typeparam>
        /// <typeparam name="TProperty">The property type of the extension.</typeparam>
        /// <param name="query">The query to extend.</param>
        /// <param name="extend">The member expression pointing to the extension.</param>
        public static Query<T> Extend<T, TProperty>(this Query<T> query, Expression<Func<T, TProperty>> extend)
            where T : class
        {
            var result = query.AsQueryInternal().Clone<Query<T>>();
            var expr = result.AsQueryInternal().Expressions;
            expr.Extends = expr.Extends.Add(extend);
            return result;
        }

        /// <summary>
        /// Adds an extension to the query, including a nested entity in the model that would not be included by default.
        /// </summary>
        /// <typeparam name="T">The type that contains the extension.</typeparam>
        /// <typeparam name="TResult">The type of selection.</typeparam>
        /// <typeparam name="TProperty">The property type of the extension.</typeparam>
        /// <param name="query">The query to extend.</param>
        /// <param name="extend">The member expression pointing to the extension.</param>
        public static Query<T, TResult> Extend<T, TResult, TProperty>(this Query<T, TResult> query, Expression<Func<T, TProperty>> extend)
            where T : class
        {
            var result = query.AsQueryInternal().Clone<Query<T, TResult>>();
            var expr = result.AsQueryInternal().Expressions;
            expr.Extends = expr.Extends.Add(extend);
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
            var result = query.AsQueryInternal().Clone<Query<T, TResult>>();
            result.AsQueryInternal().Expressions.FilterExpression = condition;
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
            var result = query.AsQueryInternal().Clone<Query<T>>();
            result.AsQueryInternal().Expressions.FilterExpression = condition;
            return result;
        }

        /// <summary>
        /// Orders the set ascending.
        /// </summary>
        /// <typeparam name="T">The type of source entity.</typeparam>
        /// <typeparam name="TResult">The type of selection.</typeparam>
        /// <typeparam name="TMember">The type of meber to order by.</typeparam>
        /// <param name="query">The <see cref="Query{T, TResult}"/> to apply the ordering to.</param>
        /// <param name="member">A <see cref="LambdaExpression"/> selecting the member to order by.</param>
        public static Query<T, TResult> OrderBy<T, TResult, TMember>(this Query<T, TResult> query, Expression<Func<T, TMember>> member)
            where T : class
        {
            var result = query.AsQueryInternal().Clone<Query<T, TResult>>();
            result.AsQueryInternal().Expressions.OrderBy.Add((member, false));
            return result;
        }

        /// <summary>
        /// Orders the set ascending.
        /// </summary>
        /// <typeparam name="T">The type of source entity.</typeparam>
        /// <typeparam name="TMember">The type of meber to order by.</typeparam>
        /// <param name="query">The <see cref="Query{T}"/> to apply the ordering to.</param>
        /// <param name="member">A <see cref="LambdaExpression"/> selecting the member to order by.</param>
        public static Query<T> OrderBy<T, TMember>(this Query<T> query, Expression<Func<T, TMember>> member)
            where T : class
        {
            var result = query.AsQueryInternal().Clone<Query<T>>();
            result.AsQueryInternal().Expressions.OrderBy.Add((member, false));
            return result;
        }

        /// <summary>
        /// Orders the set descending.
        /// </summary>
        /// <typeparam name="T">The type of source entity.</typeparam>
        /// <typeparam name="TResult">The type of selection.</typeparam>
        /// <typeparam name="TMember">The type of meber to order by.</typeparam>
        /// <param name="query">The <see cref="Query{T, TResult}"/> to apply the ordering to.</param>
        /// <param name="member">A <see cref="LambdaExpression"/> selecting the member to order by.</param>
        public static Query<T, TResult> OrderByDescending<T, TResult, TMember>(this Query<T, TResult> query, Expression<Func<T, TMember>> member)
            where T : class
        {
            var result = query.AsQueryInternal().Clone<Query<T, TResult>>();
            result.AsQueryInternal().Expressions.OrderBy.Add((member, true));
            return result;
        }

        /// <summary>
        /// Orders the set descending.
        /// </summary>
        /// <typeparam name="T">The type of source entity.</typeparam>
        /// <typeparam name="TMember">The type of meber to order by.</typeparam>
        /// <param name="query">The <see cref="Query{T}"/> to apply the ordering to.</param>
        /// <param name="member">A <see cref="LambdaExpression"/> selecting the member to order by.</param>
        public static Query<T> OrderByDescending<T, TMember>(this Query<T> query, Expression<Func<T, TMember>> member)
            where T : class
        {
            var result = query.AsQueryInternal().Clone<Query<T>>();
            result.AsQueryInternal().Expressions.OrderBy.Add((member, true));
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
            var result = query.AsQueryInternal().Clone<Query<T, TResult>>();
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
            var result = query.AsQueryInternal().Clone<Query<T>>();
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
            var result = query.AsQueryInternal().Clone<Query<T, TResult>>();
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
            var result = query.AsQueryInternal().Clone<Query<T>>();
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
        public static QueryArrayResponse<TResult> ParseJson<T, TResult>(this Query<T, TResult> query, string json)
            where T : class
        {
            var resultMapping = query.AsQueryInternal().ResultMapping;
            if (resultMapping == null)
            {
                var selectClause = (query.AsQueryInternal().Expressions?.SelectClause as ExpressionSelectClause)?.Expression;
                var visitor = new SelectSerializer<T, TResult>(selectClause ?? throw new Exception("No Select expression present"));
                resultMapping = visitor.ResultMapping;
            }

            var serializerOptions = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
            var o = JsonNode.Parse(json);
            return new QueryArrayResponse<TResult>
                (
                    JsonSerializer.Deserialize<QueryData>(o["query"], serializerOptions),
                    o["data"].AsArray()?.Select(resultMapping).ToArray()
                );
        }

        /// <summary>
        /// Parses a received json message to the <typeparamref name="T"/> type.
        /// </summary>
        /// <typeparam name="T">The type of source entity.</typeparam>
        /// <param name="query">The <see cref="Query{T, TResult}"/> used to request the received json.</param>
        /// <param name="json">The received json.</param>
        /// <returns>An array of <typeparamref name="T"/>.</returns>
        public static QueryArrayResponse<T> ParseJson<T>(this Query<T> query, string json)
            where T : class
        {
            var serializerOptions = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
            var o = JsonNode.Parse(json);
            return new QueryArrayResponse<T>
                (
                    JsonSerializer.Deserialize<QueryData>(o["query"], serializerOptions),
                    JsonSerializer.Deserialize<T[]>(o["data"], serializerOptions)
                );
        }
    }
}
