using Mt.GraphQL.Api.Test.Web.NetFramework.EF;
using Mt.GraphQL.Api.Test.Web.NetFramework.Models;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Web.Mvc;

namespace Mt.GraphQL.Api.Test.Web.NetFramework.Controllers
{
    public class MvcController : Controller
    {
        // GET: Mvc
        public ActionResult Index(Query<Entity> query)
        {
            if (!ModelState.IsValid)
                return ModelState.ToBadRequest(ControllerContext);

            try
            {
                using (var context = new DbContext())
                {
                    return Content(
                        context.Entities.Apply(query).ToJson(), 
                        "application/json");
                }
            }
            catch (QueryException ex)
            {
                Response.StatusCode = (int) HttpStatusCode.BadRequest;
                return Content(ex.Message);
            }
        }
    }
}