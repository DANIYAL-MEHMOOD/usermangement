using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MyApp.Web.Services;

namespace MyApp.Web.Pages;

public class ResetPasswordModel : PageModel
{
    private readonly IApiClient _api;
    public ResetPasswordModel(IApiClient api) => _api = api;

    [BindProperty]
    public ResetPasswordInput Input { get; set; } = new();

    public string? StatusMessage { get; set; }
    public bool Success { get; set; }

    public class ResetPasswordInput
    {
        public string Token { get; set; } = string.Empty;
        public string NewPassword { get; set; } = string.Empty;
        public string ConfirmNewPassword { get; set; } = string.Empty;
    }

    public void OnGet(string token)
    {
        Input.Token = token;
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var result = await _api.PostAsync<ResetPasswordInput, object>("api/auth/reset-password", Input);
        Success = result?.Success ?? false;
        StatusMessage = result?.Message ?? "Unable to reset password.";
        return Page();
    }
}
