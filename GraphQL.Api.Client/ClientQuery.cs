using Mt.GraphQL.Internal;
using System;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Mt.GraphQL.Api
{
    internal interface IClientQuery
    {
        ClientBase Client { get; set; }
        string Entity { get; set; }
    }

    /// <summary>
    /// Query to apply to a set of <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T">The type of items in the set.</typeparam>
    public class ClientQuery<T> : Query<T>, IClientQuery
        where T : class
    {
        private static readonly JsonSerializerOptions _jsonSerializerOptions =
            new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };

        ClientBase IClientQuery.Client { get; set; }
        string IClientQuery.Entity { get; set; }

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
            {
                cq.Entity = ((IClientQuery)this).Entity;
                cq.CopyClientFrom(this);
            }
        }

        /// <summary>
        /// Get an array of results.
        /// </summary>
        public async Task<QueryArrayResponse<T>> ToArrayAsync()
        {
            var json = await this.GetClient().FetchDataAsync(((IClientQuery)this).Entity, ToString());
            return this.ParseJson(json);
        }

        /// <summary>
        /// Gets the number of results.
        /// </summary>
        public async Task<QueryCountResponse> CountAsync()
        {
            Count = true;
            return JsonSerializer.Deserialize<QueryCountResponse>(await this.GetClient().FetchDataAsync(((IClientQuery)this).Entity, ToString()), _jsonSerializerOptions);
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
        string IClientQuery.Entity { get; set; }

        /// <summary>
        /// Copies the querie's properties to <paramref name="destination"/>.
        /// </summary>
        /// <param name="destination">The object to copy the properties to.</param>
        protected override void CopyPropertiesTo(IQueryInternal<T> destination)
        {
            base.CopyPropertiesTo(destination);
            if (destination is IClientQuery cq)
            {
                cq.Entity = ((IClientQuery)this).Entity;
                cq.CopyClientFrom(this);
            }
        }

        /// <summary>
        /// Get an array of results.
        /// </summary>
        public async Task<QueryArrayResponse<TResult>> ToArrayAsync() =>
            this.ParseJson(await this.GetClient().FetchDataAsync(((IClientQuery)this).Entity, ToString()));
    }
}
