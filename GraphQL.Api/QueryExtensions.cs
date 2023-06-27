using Newtonsoft.Json.Linq;
using System;
using System.Linq;
using System.Linq.Expressions;

namespace Mt.GraphQL.Api
{
    public static class QueryExtensions
    {
        public static Query<T, TResult> Select<T, TResult>(this Query<T> query, Expression<Func<T, TResult>> selection)
            where T : class
        {
            // Validate expression
            new SelectSerializer<T>(selection);

            // Create result
            var result = query.Clone<Query<T, TResult>>();
            result.Expressions.SelectExpression = selection;
            return result;
        }

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

        public static Query<T> Skip<T>(this Query<T> query, int skip)
            where T : class
        {
            var result = query.Clone<Query<T>>();
            result.Skip = skip;
            return result;
        }

        public static Query<T, TResult> Skip<T, TResult>(this Query<T, TResult> query, int skip)
            where T : class
        {
            var result = query.Clone<Query<T, TResult>>();
            result.Skip = skip;
            return result;
        }

        public static Query<T> Take<T>(this Query<T> query, int take)
            where T : class
        {
            var result = query.Clone<Query<T>>();
            result.Take = take;
            return result;
        }

        public static Query<T, TResult> Take<T, TResult>(this Query<T, TResult> query, int take)
            where T : class
        {
            var result = query.Clone<Query<T, TResult>>();
            result.Take = take;
            return result;
        }

        public static TResult[] ParseJson<T, TResult>(this Query<T, TResult> query, string json)
            where T : class
        {
            var visitor = new SelectSerializer<T, TResult>(
                query.Expressions?.SelectExpression 
                ?? throw new Exception("No Select expression present"));

            return JArray.Parse(json).Select(visitor.ResultMapping).ToArray();
        }
    }
}
