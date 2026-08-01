using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MyApp.Web.Services;

namespace MyApp.Web.Filters;

/// <summary>Screens (User Management, Roles, Permission Matrix, Menu Management)
/// restricted to the Administrator role client-side — the API enforces the same
/// restriction server-side via [Authorize(Roles = "Administrator")], so this is
/// a UX guard, not the security boundary.</summary>
public class AdminOnlyFilter : IAsyncPageFilter
{
    public Task OnPageHandlerSelectionAsync(PageHandlerSelectedContext context) => Task.CompletedTask;

    public async Task OnPageHandlerExecutionAsync(PageHandlerExecutingContext context, PageHandlerExecutionDelegate next)
    {
        var role = context.HttpContext.Session.GetString(SessionKeys.RoleName);
        if (!string.Equals(role, "Administrator", StringComparison.OrdinalIgnoreCase))
        {
            context.Result = new RedirectToPageResult("/Dashboard");
            return;
        }

        await next();
    }
}
