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
            // Code that runs on application startup
            GlobalConfiguration.Configure(WebApiConfig.Register);
            RouteConfig.RegisterRoutes(RouteTable.Routes);            
        }
    }
}