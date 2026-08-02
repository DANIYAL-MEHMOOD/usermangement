using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using MyApp.Web.Common;

namespace MyApp.Web.Filters;

/// <summary>
/// Bounces to /Auth/Login if the session has no API token — applied to
/// controllers or actions that require a signed-in user.
/// </summary>
public class AuthGuardFilter : IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var token = context.HttpContext.Session.GetString(SessionKeys.ApiToken);
        if (string.IsNullOrEmpty(token))
        {
            context.Result = new RedirectToActionResult("Login", "Auth", new { returnUrl = context.HttpContext.Request.Path });
            return;
        }

        await next();
    }
}
