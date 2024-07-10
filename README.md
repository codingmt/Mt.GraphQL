# Introduction
Using GraphQL queries, the client of an API can control how entities are returned by applying custom filtering, sorting, paging and/or selecting just one or a few of the entity's properties, all specified in the request. [Mt.GraphQL](https://www.nuget.org/packages?q=Mt.GraphQL) contains libraries for exposing and querying APIs using GraphQL. Client side, a request can be built using Linq-like expressions. Server-side, the query can be applied directly to `IQueryables<>` from EntityFramework.

# Client
For the client side, package [Mt.GraphQL.Api.Client](https://www.nuget.org/packages/Mt.GraphQL.Api.Client) is used. A client class can be created to expose queries on entities. Using those queries, you can select a part of the entity's properties, filter the results, sort them and/or apply paging.
```c#
class GraphQlClient : ClientBase
{
    public ClientQuery<Contact> Contacts => CreateQuery<Contact>();
}

var client = new GraphQlClient();

var result = await client.Contacts
    .Select(x => new { x.Id, x.Name, x.DateOfBirth })
    .Where(x => x.Name.StartsWith("Contact 1"))
    .OrderByDescending(x => x.Name)
    .Skip(1)
    .ToArrayAsync();
// Uses query string: ?select=Id,Name,DateOfBirth&filter=startsWith(Name,'Contact 1')&orderBy=Id desc&skip=1
```

# Server
For the server side, package [Mt.GraphQL.Api.Server](https://www.nuget.org/packages/Mt.GraphQL.Api.Server) is used. To receive the GraphQL query, a generic Query class is used as an argument for the GET methods. This argument can be applied directly to an `IEnumerable<>` or an `IQueryable<>`, for example coming from EntityFramework's data context.

## ASP.Net Core
```c#
[ApiController, Route("Contact")]
public class ContactController : ControllerBase
{
    [HttpGet]
    public ActionResult Get([FromQuery] Query<Contact> query)
    {
        // This part is only needed when the controller has no ApiController attribute.
        if (!ModelState.IsValid)
            return ModelState.ToBadRequest(ControllerContext);

        try
        {
            using var context = new DbContext();
            return Ok(context.Contacts.Apply(query));
        }
        catch (QueryException ex)
        {
            return BadRequest(ex.Message);
        }
    }
}
```

## .NET Framework MVC 5
```c#
[HttpGet, Route("Contact")]
public ActionResult GetContacts(Query<Contact> query)
{
    if (!ModelState.IsValid)
        return ModelState.ToBadRequest(ControllerContext);

    try
    {
        using (var context = new DbContext())
        {
            return Json(context.Contacts.Apply(query));
        }
    }
    catch (QueryException ex)
    {
        Response.StatusCode = (int)HttpStatusCode.BadRequest;
        return Content(ex.Message);
    }
}
```

## .NET Framework API
```c#
[HttpGet, Route("Contact")]
public IHttpActionResult GetContacts([FromUri]Query<Contact> query)
{
    if (!ModelState.IsValid)
        return BadRequest(ModelState);

    try
    {
        using (var context = new DbContext())
        {
            return Ok(context.Contacts.Apply(query));
        }
    }
    catch (QueryException ex)
    {
        return BadRequest(ex.Message);
    }
}
```

## Server configuration
To customize the way entities can be queried, they can be configured in the startup of the API using `GraphqlConfiguration.Configure<>()`. You can configure 
- the maximum page size, globally or per entity
- which column to order by if not specified in the query
- which columns can be used for fitering and sorting (to avoid poorly performing queries which filter on database columns that aren't indexed)
- which serialization attributes should be applied to properties
- which properties should be hidden
- which navigation properties are Extensions (these properties are not returned by default, only when explicitly requested using an `extend` clause. See the [Mt.GraphQL.Api.Client](https://www.nuget.org/packages/Mt.GraphQL.Api.Client) README for more info).

Configurations can also be applied to base classes using `GraphqlConfiguration.ConfigureBase<>()`. This way the configuration is applied to all classes that derive from the configured base class.
```c#
GraphqlConfiguration.DefaultMaxPageSize = 200;
GraphqlConfiguration.ConfigureBase<ModelBase>()
    .DefaultOrderBy(x => x.Id)
    .AllowFilteringAndSorting(x => x.Id);
GraphqlConfiguration.Configure<Customer>()
    .AllowFilteringAndSorting(x => x.Name)
    .ExcludeProperty(x => x.Contacts.First().Customer)
    .ExcludeProperty(x => x.Contacts.First().Customer_Id);
GraphqlConfiguration.Configure<Contact>()
    .AllowFilteringAndSorting(x => x.Name)
    .AllowFilteringAndSorting(x => x.Customer_Id)
    .ApplyAttribute(
        x => x.DateOfBirth,
        () => new JsonDateTimeConverterAttribute { Format = "yyyy-MM-dd" })
    .ExcludeProperty(x => x.Customer.Contacts)
    .IsExtension(x => x.Customer);
```
