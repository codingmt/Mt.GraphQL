using Mt.GraphQL.Api.Server;
using Mt.GraphQL.Api.Test.Web.Core.Helpers;
using Mt.GraphQL.Api.Test.Web.Core.Models;
using System.Web.Mvc;

namespace Mt.GraphQL.Api.Test.Web.Core
{
    internal class Program
    {
        static Program()
        {
            GraphqlConfiguration.DefaultMaxPageSize = 200;
            GraphqlConfiguration.ConfigureBase<ModelBase>()
                .AllowFilteringAndSorting(x => x.Id)
                .ExcludeProperty(x => x.CreatedDate);
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
                .ExcludeProperty(x => x.Customer_Id)
                .ExcludeProperty(x => x.Customer.Code)
                .IsExtension(x => x.Customer);
        }

        internal static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.

            // Only needed when we use ApiController attribute on controllers
            builder.Services.AddControllers()
                .AddJsonOptions(
                    opt =>
                    {
                        opt.JsonSerializerOptions.WriteIndented = true;
                        opt.JsonSerializerOptions.DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;
                    })
                .ConfigureApiBehaviorOptions(opt => 
                    opt.InvalidModelStateResponseFactory = ctx => ctx.ModelState.ToBadRequest(ctx));

            var app = builder.Build();

            // Configure the HTTP request pipeline.

            //app.UseHttpsRedirection();

            //app.UseAuthorization();

            app.MapControllers();

            app.Run();
        }
    }
}