using Mt.GraphQL.Api.Test.Web.NetFramework.EF;
using Mt.GraphQL.Api.Test.Web.NetFramework.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Web.Mvc;

namespace Mt.GraphQL.Api.Test.Web.NetFramework.Controllers
{
    public class MvcController : Controller
    {
        [HttpGet, Route("Mvc/Customer")]
        public ActionResult GetCustomers(Query<Customer> query) => Get(c => c.Customers, query);

        [HttpGet, Route("Mvc/Contact")]
        public ActionResult GetContacts(Query<Contact> query) => Get(c => c.Contacts, query);

        private ActionResult Get<T>(Func<DbContext, IQueryable<T>> getSet, Query<T> query)
            where T : class
        {
            if (!ModelState.IsValid)
                return ModelState.ToBadRequest(ControllerContext);

            try
            {
                using (var context = new DbContext())
                {
                    return Json(getSet(context).Apply(query), JsonRequestBehavior.AllowGet);
                }
            }
            catch (QueryException ex)
            {
                Response.StatusCode = (int)HttpStatusCode.BadRequest;
                return Content(ex.Message);
            }
        }
    }
}