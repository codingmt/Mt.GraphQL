using Microsoft.AspNetCore.Mvc;
using Mt.GraphQL.Api.Test.Web.Core.EF;
using Mt.GraphQL.Api.Test.Web.Core.Models;
using System.Web.Mvc;

namespace Mt.GraphQL.Api.Test.Web.Core.Controllers
{
    [ApiController]
    [Route("Entity")]
    public class EntityController : ControllerBase
    {
        [HttpGet]
        public ActionResult Get([FromQuery] Query<Entity> query)
        {
            // This part is only needed when the controller has no ApiController attribute.
            if (!ModelState.IsValid)
                return ModelState.ToBadRequest(ControllerContext);

            try
            {
                using var context = new DbContext();
                return Content(context.Entities.Apply(query).ToJson(), "application/json");
            }
            catch (QueryException ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }
}