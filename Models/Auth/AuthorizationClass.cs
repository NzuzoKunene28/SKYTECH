using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace SkyTech.Models
{
    // Core uses IActionFilter instead of System.Web.Mvc.ActionFilterAttribute
    public class AuthorizeUserAttribute : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            // Session.GetInt32 returns null if key is absent
            var userId = context.HttpContext.Session.GetInt32("UserId");

            if (userId == null)
            {
                context.Result = new RedirectToActionResult("LogIn", "Authentication", null);
            }

            base.OnActionExecuting(context);
        }
    }
}