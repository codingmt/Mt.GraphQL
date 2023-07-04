using Newtonsoft.Json.Linq;
using System;
using System.Linq.Expressions;
using System.Text;

namespace Mt.GraphQL.Api
{
    /// <summary>
    /// Query to apply to a set of <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T">The type of items in the set.</typeparam>
    public class Query<T>
        where T : class
    {
        /// <summary>
        /// The fields to select from type <typeparamref name="T"/>.
        /// </summary>
        public string? Select
        {
            get => Expressions.GetSelect();
            set => Expressions.ParseSelect(value ?? string.Empty);
        }

        /// <summary>
        /// The filter to apply to the set of type <typeparamref name="T"/>.
        /// </summary>
        public string? Filter
        {
            get => Expressions.GetFilter();
            set => Expressions.ParseFilter(value ?? string.Empty);
        }

        /// <summary>
        /// The number of items to skip.
        /// </summary>
        public int? Skip { get; set; }

        /// <summary>
        /// The number of items to take.
        /// </summary>
        public int? Take { get; set; }

        internal QueryExpressions<T> Expressions { get; } = new QueryExpressions<T>();

        /// <summary>
        /// Clones the instance.
        /// </summary>
        /// <typeparam name="TQuery">The type of <see cref="Query{T}"/>.</typeparam>
        public virtual TQuery Clone<TQuery>()
            where TQuery : Query<T>, new()
        {
            var myType = GetType();
            var result = (TQuery)Activator.CreateInstance(
                typeof(TQuery).IsAssignableFrom(myType)
                    ? myType
                    : typeof(TQuery));

            this.CopyPropertiesTo(result);

            return result;
        }

        /// <summary>
        /// Copies the querie's properties to <paramref name="destination"/>.
        /// </summary>
        /// <param name="destination">The object to copy the properties to.</param>
        protected virtual void CopyPropertiesTo(Query<T> destination)
        {
            destination.Expressions.SelectExpression = Expressions.SelectExpression;
            destination.Expressions.FilterExpression = Expressions.FilterExpression;
            destination.Skip = Skip;
            destination.Take = Take;
        }

        /// <summary>
        /// Returns the query to use in the URL.
        /// </summary>
        public override string ToString()
        {
            var sb = new StringBuilder();
            AddToString(sb);
            return sb.ToString();
        }

        /// <summary>
        /// Adds items to <paramref name="sb"/> to be returned by <see cref="ToString"/>.
        /// </summary>
        /// <param name="sb">The <see cref="StringBuilder"/> to add items to.</param>
        protected virtual void AddToString(StringBuilder sb)
        {
            var select = Select;
            if (!string.IsNullOrWhiteSpace(select))
            {
                if (sb.Length > 0)
                    sb.Append('&');
                sb.Append($"select={select}");
            }

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
        }

        /// <summary>
        /// Operator to implicitly convert the <see cref="Query{T}"/> to a <see cref="string"/>.
        /// </summary>
        public static implicit operator string(Query<T> from) => from.ToString();

#if DEBUG
        /// <summary>
        /// Public reference to <see cref="FilterExpression"/> for use by tests.
        /// </summary>
        public Expression? FilterExpression => Expressions.FilterExpression;
#endif
    }

    /// <summary>
    /// Query to apply to a set of <typeparamref name="T"/> resulting in type <typeparamref name="TResult"/>.
    /// </summary>
    /// <typeparam name="T">The type of items in the set.</typeparam>
    /// <typeparam name="TResult">The resulting type.</typeparam>
    public class Query<T, TResult> : Query<T>
        where T : class
    {
        internal Func<JToken, TResult>? ResultMapping { get; set; }

        /// <summary>
        /// Copies the querie's properties to <paramref name="destination"/>.
        /// </summary>
        /// <param name="destination">The object to copy the properties to.</param>
        protected override void CopyPropertiesTo(Query<T> destination)
        {
            base.CopyPropertiesTo(destination);
            if (destination is Query<T, TResult> q)
                q.ResultMapping = ResultMapping;
        }
    }
}
