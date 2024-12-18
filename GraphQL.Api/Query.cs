﻿using Mt.GraphQL.Internal;
using System;
using System.Linq;
using System.Text;
using System.Text.Json.Nodes;

namespace Mt.GraphQL.Api
{
    /// <summary>
    /// Query to apply to a set of <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T">The type of items in the set.</typeparam>
    public class Query<T> : IQueryInternal<T>, IQuery
        where T : class
    {
        private readonly QueryExpressions<T> _expressions = new QueryExpressions<T>();
        private int? _skip;
        private int? _take;

        /// <summary>
        /// The fields to select from type <typeparamref name="T"/>.
        /// </summary>
        public string Select
        {
            get => _expressions.GetSelect();
            set
            {
                try
                {
                    _expressions.ParseSelect(value ?? string.Empty);
                }
                catch (QueryInternalException ex)
                {
                    throw new QueryException(ex);
                }
            }
        }

        /// <summary>
        /// The fields to exend the model with, optionally specifiying their fields.
        /// </summary>
        /// <example>visitaddress(zipcode,housenumber,housenumberaddition)</example>
        public string Extend
        {
            get => _expressions.GetExtend();
            set
            {
                try
                {
                    _expressions.ParseExtend(value ?? string.Empty);
                }
                catch (QueryInternalException ex)
                {
                    throw new QueryException(ex);
                }
            }
        }

        /// <summary>
        /// The filter to apply to the set of type <typeparamref name="T"/>.
        /// </summary>
        public string Filter
        {
            get => _expressions.GetFilter();
            set
            {
                try
                {
                    _expressions.ParseFilter(value ?? string.Empty);
                }
                catch (QueryInternalException ex)
                {
                    throw new QueryException(ex);
                }
            }
        }

        /// <summary>
        /// The fields of type <typeparamref name="T"/> to order the resulting set by.
        /// </summary>
        public string OrderBy
        {
            get => _expressions.GetOrderBy();
            set
            {
                try
                {
                    _expressions.ParseOrderBy(value ?? string.Empty);
                }
                catch (QueryInternalException ex)
                {
                    throw new QueryException(ex);
                }
            }
        }

        /// <summary>
        /// The number of items to skip.
        /// </summary>
        public int? Skip
        {
            get => _skip;
            set
            {
                if (value < 0)
                    throw new Exception($"{nameof(Skip)} cannot be negative.");

                _skip = value;
            }
        }

        /// <summary>
        /// The number of items to take.
        /// </summary>
        public int? Take
        {
            get => _take;
            set
            {
                if (value < 0)
                    throw new Exception($"{nameof(Take)} cannot be negative.");

                _take = value;
            }
        }

        /// <summary>
        /// Indicates that the items should be counted.
        /// </summary>
        public bool? Count { get; set; }

        /// <summary>
        /// Show meta information about the entity.
        /// </summary>
        public bool? Meta { get; set; }

        QueryExpressions<T> IQueryInternal<T>.Expressions => _expressions;

        /// <summary>
        /// Clones the instance.
        /// </summary>
        /// <typeparam name="TQuery">The type of <see cref="Query{T}"/>.</typeparam>
        TQuery IQueryInternal<T>.Clone<TQuery>() => CloneInternal<TQuery>();

        /// <summary>
        /// Clones the instance.
        /// </summary>
        /// <typeparam name="TQuery">The type of <see cref="Query{T}"/>.</typeparam>
        protected virtual TQuery CloneInternal<TQuery>()
            where TQuery : IQueryInternal<T>, new()
        {
            var myType = GetType();
            var result = (TQuery)Activator.CreateInstance(
                typeof(TQuery).IsAssignableFrom(myType)
                    ? myType
                    : typeof(TQuery));

            CopyPropertiesTo(result);

            return result;
        }

        /// <summary>
        /// Copies the querie's properties to <paramref name="destination"/>.
        /// </summary>
        /// <param name="destination">The object to copy the properties to.</param>
        protected virtual void CopyPropertiesTo(IQueryInternal<T> destination)
        {
            destination.Expressions.SelectClause = _expressions.SelectClause?.Clone();
            destination.Expressions.Extends = _expressions.Extends.CloneExtends();
            destination.Expressions.FilterExpression = _expressions.FilterExpression;
            destination.Expressions.OrderBy.Clear();
            destination.Expressions.OrderBy.AddRange(_expressions.OrderBy);
            destination.Expressions.RestrictToModel = _expressions.RestrictToModel;
            destination.Skip = Skip;
            destination.Take = Take;
            destination.Count = Count;
            destination.Meta = Meta;
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
            if (Meta == true)
            {
                if (sb.Length > 0)
                    sb.Append('&');
                sb.Append($"meta=true");
                return;
            }

            var select = Select;
            if (string.IsNullOrEmpty(select) && _expressions.RestrictToModel)
                select = string.Join(",", typeof(T).GetPropertiesInheritedFirst().Select(p => p.Name));
            if (!string.IsNullOrWhiteSpace(select))
            {
                if (sb.Length > 0)
                    sb.Append('&');
                sb.Append($"select={select}");
            }

            var extend = Extend;
            if (!string.IsNullOrWhiteSpace(extend))
            {
                if (sb.Length > 0)
                    sb.Append('&');
                sb.Append($"extend={extend}");
            }

            var filter = Filter;
            if (!string.IsNullOrWhiteSpace(filter))
            {
                if (sb.Length > 0)
                    sb.Append('&');
                sb.Append($"filter={filter}");
            }

            if (Count == true)
            {
                if (sb.Length > 0)
                    sb.Append('&');
                sb.Append($"count=true");
                return;
            }

            var orderBy = OrderBy;
            if (!string.IsNullOrWhiteSpace(orderBy))
            {
                if (sb.Length > 0)
                    sb.Append('&');
                sb.Append($"orderBy={orderBy}");
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
    }

    /// <summary>
    /// Query to apply to a set of <typeparamref name="T"/> resulting in type <typeparamref name="TResult"/>.
    /// </summary>
    /// <typeparam name="T">The type of items in the set.</typeparam>
    /// <typeparam name="TResult">The resulting type.</typeparam>
    public class Query<T, TResult> : Query<T>, IQueryInternal<T, TResult>
        where T : class
    {
        Func<JsonNode, TResult> IQueryInternal<T, TResult>.ResultMapping { get; set; }

        /// <summary>
        /// Copies the querie's properties to <paramref name="destination"/>.
        /// </summary>
        /// <param name="destination">The object to copy the properties to.</param>
        protected override void CopyPropertiesTo(IQueryInternal<T> destination)
        {
            base.CopyPropertiesTo(destination);
            if (destination is Query<T, TResult> q)
                q.AsQueryInternal().ResultMapping = this.AsQueryInternal().ResultMapping;
        }
    }

    /// <summary>
    /// Specifying properties for a query.
    /// </summary>
    public interface IQuery
    {
        /// <summary>
        /// Indicates that the items should be counted.
        /// </summary>
        bool? Count { get; set; }
        /// <summary>
        /// The fields to exend the model with, optionally specifiying their fields.
        /// </summary>
        /// <example>visitaddress(zipcode,housenumber,housenumberaddition)</example>
        string Extend { get; set; }
        /// <summary>
        /// The filter to apply to the set.
        /// </summary>
        string Filter { get; set; }
        /// <summary>
        /// The fields to order the resulting set by.
        /// </summary>
        string OrderBy { get; set; }
        /// <summary>
        /// The fields to select.
        /// </summary>
        string Select { get; set; }
        /// <summary>
        /// The number of items to skip.
        /// </summary>
        int? Skip { get; set; }
        /// <summary>
        /// The number of items to take.
        /// </summary>
        int? Take { get; set; }
    }
}
