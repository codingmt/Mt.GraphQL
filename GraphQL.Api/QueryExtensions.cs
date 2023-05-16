using System;
using System.Linq.Expressions;

namespace Mt.GraphQL.Api
{
    public static class QueryExtensions
    {
        public static Query<T, TResult> Select<T, TResult>(this Query<T> query, Expression<Func<T, TResult>> selection)
        {
            // Validate expression
            var visitor = new SelectSerializer<T>();
            visitor.Visit(selection);

            // Create result
            var result = new Query<T, TResult>(query);
            result.Expressions.SelectExpression = selection;
            return result;
        }

        public static Query<T, TResult> Where<T, TResult>(this Query<T, TResult> query, Expression<Func<T, bool>> condition)
        {
            // Validate expression
            var visitor = new FilterSerializer<T>();
            visitor.Visit(condition);

            // Create result
            var result = new Query<T, TResult>(query);
            result.Expressions.FilterExpression = condition;
            return result;
        }

        public static Query<T> Where<T>(this Query<T> query, Expression<Func<T, bool>> condition)
        {
            // Validate expression
            var visitor = new FilterSerializer<T>();
            visitor.Visit(condition);

            // Create result
            var result = new Query<T>(query);
            result.Expressions.FilterExpression = condition;
            return result;
        }
    }
}
