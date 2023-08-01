using Mt.GraphQL.Api.Test.Web.NetFramework.EF;
using Mt.GraphQL.Api.Test.Web.NetFramework.Models;
using System.Linq;
using System.Web.Http;

namespace Mt.GraphQL.Api.Test.Web.NetFramework.Controllers
{
    [AllowAnonymous]
    public class ApiController : System.Web.Http.ApiController
    {
        [HttpGet]
        public IHttpActionResult Index([FromUri]Query<Entity> query)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                using(var context = new DbContext())
                {
                    return Json(
                        new
                        {
                            query,
                            data = context.Entities.Apply(query).ToJArray()
                        });
                }
            }
            catch (QueryException ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }
}