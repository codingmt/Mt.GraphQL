using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using System.Net;

namespace System.Web.Mvc
{
    public static class MvcHelpers
    {
        public static ActionResult ToBadRequest(this ModelStateDictionary modelState, ActionContext context)
        {
            context.HttpContext.Response.StatusCode = (int)HttpStatusCode.BadRequest;
            var keys = modelState.Keys.ToArray();
            return new BadRequestObjectResult(
                modelState.Values
                    .SelectMany((v, i) => v.Errors.Select(e => $"Error in {keys[i]}: {e.Exception?.Message ?? e.ErrorMessage}"))
                    .Aggregate(string.Empty, (r, v) => r + "; " + v)[2..]);
        }
    }
}