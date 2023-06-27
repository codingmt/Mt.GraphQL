using System;
using System.Linq.Expressions;
using System.Text;

namespace Mt.GraphQL.Api
{
    public class Query<T>
        where T : class
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

        internal QueryExpressions<T> Expressions { get; } = new QueryExpressions<T>();

        public virtual TQuery Clone<TQuery>()
            where TQuery : Query<T>, new()
        {
            var myType = GetType();
            var result = (TQuery)Activator.CreateInstance(
                typeof(TQuery).IsAssignableFrom(myType)
                    ? myType
                    : typeof(TQuery));

            result.Expressions.SelectExpression = Expressions.SelectExpression;
            result.Expressions.FilterExpression = Expressions.FilterExpression;
            result.Skip = Skip;
            result.Take = Take;

            return result;
        }

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
        where T : class
    {
    }
}
