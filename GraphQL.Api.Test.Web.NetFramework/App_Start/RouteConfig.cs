using System.Web.Mvc;
using System.Web.Routing;

namespace Mt.GraphQL.Api.Test.Web.NetFramework
{
    public class RouteConfig
    {
        public static void RegisterRoutes(RouteCollection routes)
        {
            routes.IgnoreRoute("{resource}.axd/{*pathInfo}");

            routes.MapRoute(
                name: "DefaultMvc",
                url: "Mvc/{controller}/{action}/{id}",
                defaults: new { controller = "Mvc", action = "Index", id = UrlParameter.Optional }
            );
        }
    }
}
