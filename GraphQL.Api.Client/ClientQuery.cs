using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Mt.GraphQL.Api
{
    internal interface IClientQuery
    {
        ClientBase? Client { get; set; }
    }

    /// <summary>
    /// Query to apply to a set of <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T">The type of items in the set.</typeparam>
    public class ClientQuery<T> : Query<T>, IClientQuery
        where T : class
    {
        ClientBase? IClientQuery.Client { get; set; }

        /// <summary>
        /// Clones the instance.
        /// </summary>
        /// <typeparam name="TQuery">The type of <see cref="Query{T}"/>.</typeparam>
        public override TQuery Clone<TQuery>()
        {
            var result = base.Clone<TQuery>();

            if (result is ClientQuery<T> cq)
                cq.CopyClientFrom(this);

            return result;
        }

        public async Task<List<T>?> ToListAsync() =>
            JsonConvert.DeserializeObject<List<T>>(await this.GetClient().FetchDataAsync(typeof(T).Name, ToString()));

        public async Task<T[]?> ToArrayAsync() =>
            JsonConvert.DeserializeObject<T[]>(await this.GetClient().FetchDataAsync(typeof(T).Name, ToString()));
    }

    /// <summary>
    /// Query to apply to a set of <typeparamref name="T"/> resulting in type <typeparamref name="TResult"/>.
    /// </summary>
    /// <typeparam name="T">The type of items in the set.</typeparam>
    /// <typeparam name="TResult">The resulting type.</typeparam>
    public class ClientQuery<T, TResult> : Query<T, TResult>, IClientQuery
        where T : class
    {
        ClientBase? IClientQuery.Client { get; set; }

        /// <summary>
        /// Clones the instance.
        /// </summary>
        /// <typeparam name="TQuery">The type of <see cref="Query{T, TResult}"/>.</typeparam>
        public override TQuery Clone<TQuery>()
        {
            var result = base.Clone<TQuery>();

            if (result is ClientQuery<T, TResult> cq)
                cq.CopyClientFrom(this);

            return result;
        }

        public async Task<List<TResult>?> ToListAsync() =>
            this.ParseJson(await this.GetClient().FetchDataAsync(typeof(T).Name, ToString())).ToList();

        public async Task<TResult[]?> ToArrayAsync() =>
            this.ParseJson(await this.GetClient().FetchDataAsync(typeof(T).Name, ToString()));
    }
}
