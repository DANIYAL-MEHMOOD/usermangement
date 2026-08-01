using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using MyApp.Web.Common;

namespace MyApp.Web.Filters;

/// <summary>
/// Screens (User Management, Roles, Permission Matrix, Menu Management)
/// restricted to the Administrator role client-side — the API enforces the same
/// restriction server-side via [Authorize(Roles = "Administrator")].
/// </summary>
public class AdminOnlyFilter : IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var role = context.HttpContext.Session.GetString(SessionKeys.RoleName);
        if (!string.Equals(role, "Administrator", StringComparison.OrdinalIgnoreCase))
        {
            context.Result = new RedirectToActionResult("AccessDenied", "Error", null);
            return;
        }

        await next();
    }
}
