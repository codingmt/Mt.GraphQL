using System.Linq;
using System.Net;

namespace System.Web.Mvc
{
    public static class MvcHelpers
    {
        public static ActionResult ToBadRequest(this ModelStateDictionary modelState, ControllerContext context)
        {
            context.HttpContext.Response.StatusCode = (int)HttpStatusCode.BadRequest;
            var keys = modelState.Keys.ToArray();
            return new ContentResult
            {
                Content = modelState.Values
                    .SelectMany((v, i) => v.Errors.Select(e => $"Error in {keys[i]}: {e.Exception?.Message ?? e.ErrorMessage}"))
                    .Aggregate(string.Empty, (r, v) => r + "; " + v)
                    .Substring(2)
            };                
        }
    }
}