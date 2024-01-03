using Mt.GraphQL.Internal;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Mt.GraphQL.Api
{
    internal interface IClientQuery
    {
        ClientBase Client { get; set; }
    }

    /// <summary>
    /// Query to apply to a set of <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T">The type of items in the set.</typeparam>
    public class ClientQuery<T> : Query<T>, IClientQuery
        where T : class
    {
        ClientBase IClientQuery.Client { get; set; }

        /// <summary>
        /// Clones the instance.
        /// </summary>
        /// <typeparam name="TQuery">The type of <see cref="Query{T}"/>.</typeparam>
        protected override TQuery CloneInternal<TQuery>()
        {
            var qType = typeof(TQuery);
            if (typeof(Query<,>) == qType.GetGenericTypeDefinition()) 
            {
                var newType = typeof(ClientQuery<,>).MakeGenericType(qType.GetGenericArguments());
                var result = (TQuery)Activator.CreateInstance(newType);
                CopyPropertiesTo(result);
                return result;
            }

            return base.CloneInternal<TQuery>();
        }

        /// <summary>
        /// Copies the querie's properties to <paramref name="destination"/>.
        /// </summary>
        /// <param name="destination">The object to copy the properties to.</param>
        protected override void CopyPropertiesTo(IQueryInternal<T> destination)
        {
            base.CopyPropertiesTo(destination);
            if (destination is IClientQuery cq)
                cq.CopyClientFrom(this);
        }

        /// <summary>
        /// Get a list of results.
        /// </summary>
        public async Task<List<T>> ToListAsync() =>
            JsonConvert.DeserializeObject<List<T>>(await this.GetClient().FetchDataAsync(typeof(T).Name, ToString()));

        /// <summary>
        /// Get an array of results.
        /// </summary>
        public async Task<T[]> ToArrayAsync() =>
            JsonConvert.DeserializeObject<T[]>(await this.GetClient().FetchDataAsync(typeof(T).Name, ToString()));

        /// <summary>
        /// Gets the number of results.
        /// </summary>
        public async Task<int> CountAsync()
        {
            var query = ToString();
            if (query.Length > 0)
                query += "&";
            query += "count=true";
            return int.Parse(await this.GetClient().FetchDataAsync(typeof(T).Name, query));
        }
    }

    /// <summary>
    /// Query to apply to a set of <typeparamref name="T"/> resulting in type <typeparamref name="TResult"/>.
    /// </summary>
    /// <typeparam name="T">The type of items in the set.</typeparam>
    /// <typeparam name="TResult">The resulting type.</typeparam>
    public class ClientQuery<T, TResult> : Query<T, TResult>, IClientQuery
        where T : class
    {
        ClientBase IClientQuery.Client { get; set; }

        /// <summary>
        /// Copies the querie's properties to <paramref name="destination"/>.
        /// </summary>
        /// <param name="destination">The object to copy the properties to.</param>
        protected override void CopyPropertiesTo(IQueryInternal<T> destination)
        {
            base.CopyPropertiesTo(destination);
            if (destination is IClientQuery cq)
                cq.CopyClientFrom(this);
        }

        /// <summary>
        /// Get a list of results.
        /// </summary>
        public async Task<List<TResult>> ToListAsync() =>
            this.ParseJson(await this.GetClient().FetchDataAsync(typeof(T).Name, ToString())).ToList();

        /// <summary>
        /// Get an array of results.
        /// </summary>
        public async Task<TResult[]> ToArrayAsync() =>
            this.ParseJson(await this.GetClient().FetchDataAsync(typeof(T).Name, ToString()));
    }
}
