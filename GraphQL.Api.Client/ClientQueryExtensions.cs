namespace Mt.GraphQL.Api
{
    internal static class ClientQueryExtensions
    {
        internal static ClientBase? GetClient(this IClientQuery clientQuery) =>
            clientQuery.Client;

        internal static void SetClient(this IClientQuery clientQuery, ClientBase client) =>
            clientQuery.Client = client;

        internal static void CopyClientFrom(this IClientQuery clientQuery, IClientQuery from) =>
            clientQuery.Client = from.Client;
    }
}
