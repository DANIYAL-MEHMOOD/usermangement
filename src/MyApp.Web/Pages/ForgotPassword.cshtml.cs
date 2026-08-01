using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MyApp.Web.Services;

namespace MyApp.Web.Pages;

public class ForgotPasswordModel : PageModel
{
    private readonly IApiClient _api;
    public ForgotPasswordModel(IApiClient api) => _api = api;

    [BindProperty]
    public ForgotPasswordInput Input { get; set; } = new();

    public bool Submitted { get; set; }

    public class ForgotPasswordInput
    {
        public string Email { get; set; } = string.Empty;
    }

    public async Task<IActionResult> OnPostAsync()
    {
        // Always shows the same "submitted" state whether or not the email
        // exists — the API deliberately returns the same response either way
        // (see AuthController.ForgotPassword) to avoid leaking registered addresses.
        await _api.PostAsync<ForgotPasswordInput, object>("api/auth/forgot-password", Input);
        Submitted = true;
        return Page();
    }
}
