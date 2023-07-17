using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace System.Web.Mvc
{
    public static class MvcHelpers
    {
        public static ActionResult ToBadRequest(this ModelStateDictionary modelState, ActionContext context)
        {
            var keys = modelState.Keys.ToArray();
            return new BadRequestObjectResult(
                modelState.Values
                    .SelectMany((v, i) => v.Errors.Select(e => $"Error in {keys[i]}: {e.Exception?.Message ?? e.ErrorMessage}"))
                    .Aggregate(string.Empty, (r, v) => r + "; " + v)[2..]);
        }
    }
}