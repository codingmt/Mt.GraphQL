# Introduction
Using GraphQL queries, the client of your API can control how entities are returned by applying custom filtering, sorting, paging and/or selecting just one or a few of the entity's properties, all specified in the request. The [Mt.GraphQL.Api.Server](https://www.nuget.org/packages/Mt.GraphQL.Api.Server) package makes it easy to expose data in your API using GraphQL queries, translating the GraphQL query to Linq to be able to apply it directly to the database, all within what is configured to be allowed.

For .NET clients, package [Mt.GraphQL.Api.Client](https://www.nuget.org/packages/Mt.GraphQL.Api.Client) can be used.

# Controllers
In your web project's controllers, the GraphQL queries are mapped to a generic `Query` object. The server library allows you to directly apply this query object to any `IQueryable` or `IEnumerable` of the entity's type. This means that you could optionally first filter the available data to make sure the client only retrieves what is allowed. You could of course also pass the query object to the business layer if your application pattern requires that. The `Apply()` function returns the requested data along with the used query parameters.

## ASP.NET Core
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

When using `ApiController` attributes on our controllers, you can remove the part that checks the model state. Instead, add the following to the application startup:
```c#
builder.Services.AddControllers()
    .ConfigureApiBehaviorOptions(
        opt => opt.InvalidModelStateResponseFactory = ctx => ctx.ModelState.ToBadRequest(ctx));
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

The used `ToBadRequest()` method is the following extension method:
```c#
public static ActionResult ToBadRequest(this ModelStateDictionary modelState, ControllerContext context)
{
    context.HttpContext.Response.StatusCode = (int)HttpStatusCode.BadRequest;
    var keys = modelState.Keys.ToArray();
    return new ContentResult
    {
        Content = modelState.Values
            .SelectMany((v, i) => v.Errors.Select(e => $"Error in {keys[i]}: {e.Exception?.Message ?? e.ErrorMessage}"))
            .Aggregate(string.Empty, (r, v) => r + "; " + v)
            .Substring(2)
    };                
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

# Server configuration
To customize the way entities can be queried, they can be configured in the startup of the API using `GraphqlConfiguration.Configure<>()`. You can configure 
- the maximum page size, globally and per entity
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

# Meta information
To get information about the data that will be returned, a meta information object can be requested using query string `?meta=true`. The response will contain property names and types, along with information whether the property is indexed or if it is an extension.
