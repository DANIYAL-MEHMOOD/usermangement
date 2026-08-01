using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MyApp.Web.Services;

namespace MyApp.Web.Filters;

/// <summary>Bounces to /Login if the session has no access token — applied to
/// folders that require a signed-in user (see Program.cs conventions).
/// Session itself expires after 30 minutes idle (Program.cs), which is what
/// actually enforces the "Session Timeout" requirement; this filter just
/// reacts to that by sending the user back to the login screen.</summary>
public class AuthGuardFilter : IAsyncPageFilter
{
    public Task OnPageHandlerSelectionAsync(PageHandlerSelectedContext context) => Task.CompletedTask;

    public async Task OnPageHandlerExecutionAsync(PageHandlerExecutingContext context, PageHandlerExecutionDelegate next)
    {
        var token = context.HttpContext.Session.GetString(SessionKeys.AccessToken);
        if (string.IsNullOrEmpty(token))
        {
            context.Result = new RedirectToPageResult("/Login");
            return;
        }

        await next();
    }
}
