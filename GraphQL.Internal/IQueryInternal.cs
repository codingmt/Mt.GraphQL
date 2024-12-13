using System;
using System.Text.Json.Nodes;

namespace Mt.GraphQL.Internal
{
    public interface IQueryInternal<T>
    {
        QueryExpressions<T> Expressions { get; }
        TQuery Clone<TQuery>() where TQuery : IQueryInternal<T>, new();
        int? Skip { get; set; }
        int? Take { get; set; }
        bool? Count { get; set; }
        bool? Meta { get; set; }
    }

    public interface IQueryInternal<T, TResult> : IQueryInternal<T>
    {
        Func<JsonNode, TResult> ResultMapping { get; set; }
    }

    public static class QueryInternalExtensions
    {
        public static IQueryInternal<T, TResult> AsQueryInternal<T, TResult>(this IQueryInternal<T, TResult> query) =>
            query;

        public static IQueryInternal<T> AsQueryInternal<T>(this IQueryInternal<T> query) =>
            query;
    }
}
