using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MyApp.Web.Services;

namespace MyApp.Web.Pages;

public class ChangePasswordModel : PageModel
{
    private readonly IApiClient _api;
    public ChangePasswordModel(IApiClient api) => _api = api;

    [BindProperty]
    public ChangePasswordInput Input { get; set; } = new();

    public string? StatusMessage { get; set; }
    public bool Success { get; set; }

    public class ChangePasswordInput
    {
        public string CurrentPassword { get; set; } = string.Empty;
        public string NewPassword { get; set; } = string.Empty;
        public string ConfirmNewPassword { get; set; } = string.Empty;
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var result = await _api.PostAsync<ChangePasswordInput, object>("api/auth/change-password", Input);

        Success = result?.Success ?? false;
        StatusMessage = result?.Message ?? "Unable to change password.";

        if (Success)
            return RedirectToPage("/Dashboard");

        return Page();
    }
}
