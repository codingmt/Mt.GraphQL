using Mt.GraphQL.Api.Test.Web.NetFramework.EF;
using Mt.GraphQL.Api.Test.Web.NetFramework.Models;
using System;
using System.Linq;
using System.Web.Http;

namespace Mt.GraphQL.Api.Test.Web.NetFramework.Controllers
{
    [AllowAnonymous]
    public class ApiController : System.Web.Http.ApiController
    {
        [HttpGet, Route("Api/Contact")]
        public IHttpActionResult GetContacts([FromUri]Query<Contact> query) => Get(c => c.Contacts, query);

        [HttpGet, Route("Api/Customer")]
        public IHttpActionResult GetCustomers([FromUri] Query<Customer> query) => Get(c => c.Customers, query);

        private IHttpActionResult Get<T>(Func<DbContext, IQueryable<T>> getSet, Query<T> query)
            where T : class
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                using (var context = new DbContext())
                {
                    return Ok(getSet(context).Apply(query));
                }
            }
            catch (QueryException ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }
}