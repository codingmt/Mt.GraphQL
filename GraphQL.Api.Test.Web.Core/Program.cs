using Mt.GraphQL.Api.Server;
using Mt.GraphQL.Api.Test.Web.Core.Models;
using System.Web.Mvc;

namespace Mt.GraphQL.Api.Test.Web.Core
{
    internal class Program
    {
        internal static void Main(string[] args)
        {
            GraphqlConfiguration.Configure<Customer>()
                .AllowFilteringAndSorting(x => x.Id)
                .AllowFilteringAndSorting(x => x.Name);
            GraphqlConfiguration.Configure<Contact>()
                .AllowFilteringAndSorting(x => x.Id)
                .AllowFilteringAndSorting(x => x.Name)
                .AllowFilteringAndSorting(x => x.Customer_Id);

            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.

            // Only needed when we use ApiController attribute on controllers
            builder.Services.AddControllers().ConfigureApiBehaviorOptions(
                opt => opt.InvalidModelStateResponseFactory =
                    ctx => ctx.ModelState.ToBadRequest(ctx));

            var app = builder.Build();

            // Configure the HTTP request pipeline.

            //app.UseHttpsRedirection();

            //app.UseAuthorization();

            app.MapControllers();

            app.Run();
        }
    }
}