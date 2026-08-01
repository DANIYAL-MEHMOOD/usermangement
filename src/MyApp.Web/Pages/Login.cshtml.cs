using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MyApp.Web.Services;

namespace MyApp.Web.Pages;

public class LoginModel : PageModel
{
    private readonly IApiClient _api;

    public LoginModel(IApiClient api) => _api = api;

    [BindProperty]
    public LoginInput Input { get; set; } = new();

    public string? ErrorMessage { get; set; }

    public class LoginInput
    {
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public bool RememberMe { get; set; }
    }

    private record LoginResponseData(string AccessToken, string RefreshToken, DateTime AccessTokenExpiry,
        int UserId, string Username, string FullName, string RoleName, bool MustChangePassword);

    public async Task<IActionResult> OnPostAsync()
    {
        var result = await _api.PostAsync<LoginInput, LoginResponseData>("api/auth/login", Input);

        if (result is null || !result.Success || result.Data is null)
        {
            ErrorMessage = result?.Message ?? "Unable to sign in. Please try again.";
            return Page();
        }

        HttpContext.Session.SetString(SessionKeys.AccessToken, result.Data.AccessToken);
        HttpContext.Session.SetString(SessionKeys.RefreshToken, result.Data.RefreshToken);
        HttpContext.Session.SetString(SessionKeys.AccessTokenExpiry, result.Data.AccessTokenExpiry.ToString("o"));
        HttpContext.Session.SetInt32(SessionKeys.UserId, result.Data.UserId);
        HttpContext.Session.SetString(SessionKeys.Username, result.Data.Username);
        HttpContext.Session.SetString(SessionKeys.FullName, result.Data.FullName);
        HttpContext.Session.SetString(SessionKeys.RoleName, result.Data.RoleName);

        if (result.Data.MustChangePassword)
            return RedirectToPage("/ChangePassword");

        return RedirectToPage("/Dashboard");
    }
}
