using Mt.GraphQL.Api.Server;
using Mt.GraphQL.Api.Test.Web.NetFramework.Helpers;
using Mt.GraphQL.Api.Test.Web.NetFramework.Models;
using System;
using System.Web;
using System.Web.Http;
using System.Web.Routing;

namespace Mt.GraphQL.Api.Test.Web.NetFramework
{
    public class Global : HttpApplication
    {
        void Application_Start(object sender, EventArgs e)
        {
            GlobalConfiguration.Configuration.Formatters.JsonFormatter.SerializerSettings
                .ReferenceLoopHandling = Newtonsoft.Json.ReferenceLoopHandling.Ignore;
            GlobalConfiguration.Configuration.Formatters
                .Remove(GlobalConfiguration.Configuration.Formatters.XmlFormatter);  
            
            // Code that runs on application startup
            GlobalConfiguration.Configure(WebApiConfig.Register);
            RouteConfig.RegisterRoutes(RouteTable.Routes);

            GraphqlConfiguration.Configure<Customer>()
                .AllowFilteringAndSorting(x => x.Id)
                .AllowFilteringAndSorting(x => x.Name);
            GraphqlConfiguration.Configure<Contact>()
                .AllowFilteringAndSorting(x => x.Id)
                .AllowFilteringAndSorting(x => x.Name)
                .AllowFilteringAndSorting(x => x.Customer_Id)
                .ApplyAttribute(x => x.DateOfBirth, () => new Newtonsoft.Json.JsonConverterAttribute(typeof(JsonDateOnlyConverter)));
        }
    }
}