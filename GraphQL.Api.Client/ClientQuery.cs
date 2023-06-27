using System;

namespace Mt.GraphQL.Api
{
    public class ClientQuery<T> : Query<T>
        where T : class
    {
        private Client? _client;

        public ClientQuery(Client client) : base()
        {
            _client = client;
        }

        public override TQuery Clone<TQuery>()
        {
            var result =
                typeof(TQuery).GetGenericTypeDefinition() == typeof(Query<,>)
                    ? (TQuery)Activator.CreateInstance(typeof(ClientQuery<,>).MakeGenericType(typeof(TQuery).GetGenericArguments()), _client)
                    : base.Clone<TQuery>();
    
            if (result is ClientQuery<T> cq) 
                cq._client = _client;

            return result;
        }
    }

    public class ClientQuery<T, TResult> : Query<T, TResult>
        where T : class
    {
        private Client? _client;

        public ClientQuery(Client client) : base()
        {
            _client = client;
        }

        public override TQuery Clone<TQuery>()
        {
            var result = base.Clone<TQuery>();

            if (result is ClientQuery<T, TResult> cq)
                cq._client = _client;

            return result;
        }
    }
}
