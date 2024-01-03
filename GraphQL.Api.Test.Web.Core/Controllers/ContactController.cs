using Microsoft.AspNetCore.Mvc;
using Mt.GraphQL.Api.Test.Web.Core.EF;
using Mt.GraphQL.Api.Test.Web.Core.Models;
using System.Web.Mvc;

namespace Mt.GraphQL.Api.Test.Web.Core.Controllers
{
    [ApiController]
    [Route("Contact")]
    public class ContactController : ControllerBase
    {
        [HttpGet]
        public ActionResult Get([FromQuery] Query<Contact> query)
        {
            // This part is only needed when the controller has no ApiController attribute.
            if (!ModelState.IsValid)
                return ModelState.ToBadRequest(ControllerContext);

            try
            {
                using var context = new DbContext();
                return Ok(context.Contacts.Apply(query));
            }
            catch (QueryException ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }
}