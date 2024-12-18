using Mt.GraphQL.Api.Server;
using Mt.GraphQL.Api.Test.Web.Core.Helpers;
using Mt.GraphQL.Api.Test.Web.Core.Models;
using System.Web.Mvc;

namespace Mt.GraphQL.Api.Test.Web.Core
{
    internal class Program
    {
        // Test project starts webserver multiple times, duplicating the configuration.
        private static bool isConfigured = false;

        private static void ConfigureGraphQL(ConfigurationManager configurationManager)
        {
            if (isConfigured) return;

            GraphqlConfiguration.FromConfiguration(configurationManager);
            GraphqlConfiguration.Configure<Contact>()
                .ApplyAttribute(
                    x => x.DateOfBirth,
                    () => new JsonDateTimeConverterAttribute { Format = "yyyy-MM-dd" });

            isConfigured = true;
        }

        internal static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            ConfigureGraphQL(builder.Configuration);
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