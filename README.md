# Introduction
Mt.GraphQL contains libraries for exposing and querying APIs using GraphQL. Client side, a request can be built using Linq-like expressions. Server-side, the query can be applied directly to `IQueryables<>` from EntityFramework.

# Client
For the client side, package [Mt.GraphQL.Api.Client](https://www.nuget.org/packages/Mt.GraphQL.Api.Client) is used. A client class can be created to expose queries on entities. Using those queries, you can partially select the entity, filter it, sort it and apply paging. Results can be received in arrays, lists or as an int by calling `CountAsync()`.
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
```

# Server
For the server side, package [Mt.GraphQL.Api.Server](https://www.nuget.org/packages/Mt.GraphQL.Api.Server) is used. To receive the GraphQL query, a generic Query class is used as an argument for the GET methods. This query can be applied directly to a `IQueryable<>` coming from EntityFramework's data context, or a hard-coded filter can be applied to the `IQueryable<>` first.

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
To customize the way entities can be queried, they can be configured in the startup of the API. You can configure 
- the maximum page size, globally or per entity
- which columns can be used for fitering and sorting (to avoid poorly performing queries which filter on columns that aren't indexed)
- which serialization attributes should be applied to properties
- which properties should be hidden.
```c#
GraphqlConfiguration.DefaultMaxPageSize = 200;
GraphqlConfiguration.Configure<Customer>()
    .AllowFilteringAndSorting(x => x.Id)
    .AllowFilteringAndSorting(x => x.Name)
    .ExcludeProperty(x => x.Contacts.First().Customer)
    .ExcludeProperty(x => x.Contacts.First().Customer_Id);
GraphqlConfiguration.Configure<Contact>()
    .AllowFilteringAndSorting(x => x.Id)
    .AllowFilteringAndSorting(x => x.Name)
    .AllowFilteringAndSorting(x => x.Customer_Id)
    .ApplyAttribute(
        x => x.DateOfBirth,
        () => new JsonDateTimeConverterAttribute { Format = "yyyy-MM-dd" })
    .ExcludeProperty(x => x.Customer_Id)
    .ExcludeProperty(x => x.Customer.Contacts);
```
