using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MyApp.Web.Services;

namespace MyApp.Web.Pages;

public class LogoutModel : PageModel
{
    private readonly IApiClient _api;
    public LogoutModel(IApiClient api) => _api = api;

    public async Task<IActionResult> OnPostAsync()
    {
        var refreshToken = HttpContext.Session.GetString(SessionKeys.RefreshToken);
        if (!string.IsNullOrEmpty(refreshToken))
        {
            // Best-effort — revoke server-side so the refresh token can't be replayed;
            // the session is cleared regardless of whether this call succeeds.
            await _api.PostAsync<object, object>("api/auth/logout", new { RefreshToken = refreshToken });
        }

        HttpContext.Session.Clear();
        return RedirectToPage("/Login");
    }

    public IActionResult OnGet() => RedirectToPage("/Login");
}
