using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace Mt.GraphQL.Api
{
    /// <summary>
    /// Interface of a query response object.
    /// </summary>
    public class QueryResponse<T>
    {
        /// <summary>
        /// The query data.
        /// </summary>
        public QueryData Query { get; set; }
        /// <summary>
        /// The data of the response.
        /// </summary>
        public T Data { get; set; }

        /// <summary>
        /// Creates a new <see cref="QueryResponse{T}"/>.
        /// </summary>
        public QueryResponse() { }

        /// <summary>
        /// Creates a new <see cref="QueryResponse{T}"/>.
        /// </summary>
        public QueryResponse(IQuery query, T data)
        {
            Query = new QueryData(query);
            Data = data;
        }
    }

    /// <summary>
    /// Response object for queries, including the query and the data.
    /// </summary>
    /// <typeparam name="T">The type of data.</typeparam>
    public class QueryArrayResponse<T> : QueryResponse<T[]>, IList, IList<T>
    {
        /// <summary>
        /// The length of the array.
        /// </summary>
        public int Length => Data.Length;

        /// <inheritdoc/>
        public T this[int index] 
        { 
            get => Data[index]; 
            set => Data[index] = value; 
        }

        object IList.this[int index]
        {
            get => Data[index];
            set
            {
                if (value is T v)
                    Data[index] = v;
                else
                    throw new ArgumentException($"Value must be of type {typeof(T).FullName}.");
            }
        }

        /// <summary>
        /// Creates a new <see cref="QueryArrayResponse{T}"/> object from a query and the resulting data.
        /// </summary>
        /// <param name="query">The query.</param>
        /// <param name="data">The data.</param>
        public QueryArrayResponse(IQuery query, T[] data) : base(query, data) { }

        /// <summary>
        /// Creates a new <see cref="QueryArrayResponse{T}"/> object.
        /// </summary>
        public QueryArrayResponse() { }

        bool IList.IsFixedSize => Data.IsFixedSize;
        bool IList.IsReadOnly => Data.IsReadOnly;
        bool ICollection<T>.IsReadOnly => Data.IsReadOnly;
        int ICollection.Count => Data.Length;
        int ICollection<T>.Count => Data.Length;
        bool ICollection.IsSynchronized => Data.IsSynchronized;
        object ICollection.SyncRoot => Data.SyncRoot;
        int IList.Add(object value) => throw new NotImplementedException();
        void IList.Clear() => throw new NotImplementedException();
        void ICollection<T>.Clear() => throw new NotImplementedException();
        bool IList.Contains(object value) => throw new NotImplementedException();
        int IList.IndexOf(object value) => throw new NotImplementedException();
        void IList.Insert(int index, object value) => throw new NotImplementedException();
        void IList.Remove(object value) => throw new NotImplementedException();
        void IList.RemoveAt(int index) => throw new NotImplementedException();
        void IList<T>.RemoveAt(int index) => throw new NotImplementedException();
        void ICollection.CopyTo(Array array, int index) => Data.CopyTo(array, index);
        IEnumerator IEnumerable.GetEnumerator() => Data.GetEnumerator();
        int IList<T>.IndexOf(T item) => throw new NotImplementedException();
        void IList<T>.Insert(int index, T item) => throw new NotImplementedException();
        void ICollection<T>.Add(T item) => throw new NotImplementedException();
        bool ICollection<T>.Contains(T item) => throw new NotImplementedException();
        void ICollection<T>.CopyTo(T[] array, int arrayIndex) => Data.CopyTo(array, arrayIndex);
        bool ICollection<T>.Remove(T item) => throw new NotImplementedException();
        IEnumerator<T> IEnumerable<T>.GetEnumerator() => throw new NotImplementedException();
    }

    /// <summary>
    /// Response object for queries, including the query and the data.
    /// </summary>
    public class QueryCountResponse : QueryResponse<int>
    {
        /// <summary>
        /// The count.
        /// </summary>
        public int Count { get => Data; set => Data = value; }

        /// <summary>
        /// Operator to implicitly calculate with the response.
        /// </summary>
        public static implicit operator int(QueryCountResponse from) => from.Count;
    }

    /// <summary>
    /// Contains data about the query.
    /// </summary>
    public class QueryData : IQuery
    {
        /// <summary>
        /// Creates a new <see cref="QueryData"/> object.
        /// </summary>
        public QueryData() { }

        internal QueryData(IQuery query) 
        {
            Select = nullIfEmpty(query.Select);
            Extend = nullIfEmpty(query.Extend);
            Filter = nullIfEmpty(query.Filter);
            OrderBy = nullIfEmpty(query.OrderBy);
            Skip = nullIf0(query.Skip);
            Take = nullIf0(query.Take);
            Count = nullIfFalse(query.Count);

            string nullIfEmpty(string s) => string.IsNullOrEmpty(s) ? null : s;
            int? nullIf0(int? i) => i == 0 ? null : i;
            bool? nullIfFalse(bool? i) => i == true ? (bool?)true : null;
        }

        /// <summary>
        /// The fields to select.
        /// </summary>
        public string Select { get; set; }
        /// <summary>
        /// The fields to exend the model with, optionally specifiying their fields.
        /// </summary>
        /// <example>visitaddress(zipcode,housenumber,housenumberaddition)</example>
        public string Extend { get; set; }
        /// <summary>
        /// The filter to apply to the set.
        /// </summary>
        public string Filter { get; set; }
        /// <summary>
        /// The fields to order the resulting set by.
        /// </summary>
        public string OrderBy { get; set; }
        /// <summary>
        /// The number of items to skip.
        /// </summary>
        public int? Skip { get; set; }
        /// <summary>
        /// The number of items to take.
        /// </summary>
        public int? Take { get; set; }
        /// <summary>
        /// Indicates that the items should be counted.
        /// </summary>
        public bool? Count { get; set; }

        /// <inheritdoc/>
        public override string ToString()
        {
            var sb = new StringBuilder();

            var select = Select;
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
                return sb.ToString();
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

            return sb.ToString();
        }
    }
}
