# Introduction
The [Mt.GraphQL.Api.Client](https://www.nuget.org/packages/Mt.GraphQL.Api.Client) package allows developers to call a Mt.GraphQL enabled API using intuitive Linq-like expressions. Use the [Mt.GraphQL.Api.Server](https://www.nuget.org/packages/Mt.GraphQL.Api.Server) package and check it's README to set up the server.

# Setting up a client class
Set up the client class by deriving from the `ClientBase` class. The `CreateQuery()` method is used to create a query on a client entity.
```c#
use Mt.GraphQL.Api;

class GraphQlClient : ClientBase
{
    public ClientQuery<Contact> Contacts => CreateQuery<Contact>();
}
```

# Retrieving the data
You can retrieve data in an array or a list. The number of results could be limited by the server configuration. In that case you need to use paging (see below) to retrieve more than one page of data.
```c#
var client = new GraphQlClient();
var data = await client.Contacts
    .ToArrayAsync(); // query will be: /Contact
```

# Selecting a part of the model
You can select only the data needed for your application, instead of the complete model. You can also select fields of related entities. The result type will be an array of the specified anonymous type.
```c#
var client = new GraphQlClient();
var data = await client.Contacts
    .Select(x => new { x.Id, x.Name, x.VisitAddress.ZipCode })
    .ToArrayAsync(); // query will be: /Contact?select=Id,Name,VisitAddress.ZipCode
```

# Filtering
You can filter data on properties which are set up for filtering and sorting on the server by adding a `Where` clause.
```c#
var client = new GraphQlClient();
var data = await client.Contacts
    .Where(x => x.ModifiedDate >= new DateTime(2023, 3, 29))
    .ToArrayAsync(); // query will be: /Contact?filter=ModifiedDate ge '2023-03-29'
```
More examples of filters are:
- `x => x.Id <= 5`
- `x => x.Date > new DateTime(2023, 3, 29)`
- `x => x.Name == "A"`
- `x => x.Description.StartsWith("ABC")`
- `x => !x.IsCustomer`
- `x => new []{ 1, 2, 3 }.Contains(x.Id)`
- `x => x.Id == _id || x.Id == 4 || _ids.Contains(x.Id)`
- `x => (x.Id > _id && x.Description.Contains("A")) || x.Description.EndsWith("B")`

# Sorting
You can sort data on properties which are set up for filtering and sorting on the server by adding one or more `OrderBy` or `OrderByDescending` clauses.
```c#
var client = new GraphQlClient();
var data = await client.Contacts
    .OrderBy(x => x.LastName)
    .OrderBy(x => x.FirstName)
    .ToArrayAsync(); // query will be: /Contact?orderBy=LastName,FirstName
```

# Paging
You can page your results using `Skip()` and `Take()`.

# Counting
You can count the entities by calling `CountAsync()`.

# Extends
Server side, navigation properties of entities can be marked as Extends. This means that those navigation properties will not be returned with the entity by default. If you do want these properties included, you can specify this by calling `Extend()`. This will also tell the server which properties the client type has, so nothing is returned unnecessarily.
```c#
var client = new GraphQlClient();
var data = await client.Contacts
    .Extend(x => x.VisitAddress)
    .ToArrayAsync(); // query will be /Contact?extend=VisitAddress(Street,HouseNumber,Zipcode,City)
```