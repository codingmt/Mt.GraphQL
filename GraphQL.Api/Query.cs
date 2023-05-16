﻿using System.Linq.Expressions;
using System.Text;

namespace Mt.GraphQL.Api
{
    public class Query<T>
    {
        public string? Select 
        {
            get => Expressions.GetSelect();
            set => Expressions.ParseSelect(value ?? string.Empty);
        }

        public string? Filter
        {
            get => Expressions.GetFilter();
            set => Expressions.ParseFilter(value ?? string.Empty);
        }

        public int? Skip { get; set; }

        public int? Take { get; set; }

        public Query() { }

        internal Query(Query<T> from)
        {
            Expressions.SelectExpression = from.Expressions.SelectExpression;
            Expressions.FilterExpression = from.Expressions.FilterExpression;
            Skip = from.Skip;
            Take = from.Take;
        }

        internal QueryExpressions<T> Expressions { get; } = new QueryExpressions<T>();

        public override string ToString()
        {
            var sb = new StringBuilder();

            var select = Select;
            if (!string.IsNullOrWhiteSpace(select)) 
                sb.Append($"select={select}");

            var filter = Filter;
            if (!string.IsNullOrWhiteSpace(filter))
            {
                if (sb.Length > 0)
                    sb.Append('&');
                sb.Append($"filter={filter}");
            }

            if (Skip.HasValue)
            {
                if (sb.Length > 0)
                    sb.Append('&');
                sb.Append($"skip={Skip.Value}");
            }

            if (Take.HasValue)
            {
                if (sb.Length > 0)
                    sb.Append('&');
                sb.Append($"take={Take.Value}");
            }

            return sb.ToString();
        }

        public static implicit operator string(Query<T> from) => from.ToString();

#if DEBUG
        public Expression? FilterExpression => Expressions.FilterExpression;
#endif
    }

    public class Query<T, TResult> : Query<T>
    {
        internal Query(Query<T, TResult> from)
        {
            Expressions.SelectExpression = from.Expressions.SelectExpression;
            Expressions.FilterExpression = from.Expressions.FilterExpression;
            Skip = from.Skip;
            Take = from.Take;
        }

        internal Query(Query<T> from)
        {
            Expressions.SelectExpression = from.Expressions.SelectExpression;
            Expressions.FilterExpression = from.Expressions.FilterExpression;
            Skip = from.Skip;
            Take = from.Take;
        }
    }
}
