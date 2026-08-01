using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MyApp.Web.Services;

namespace MyApp.Web.Pages.Settings;

public class ThemeModel : PageModel
{
    private readonly IApiClient _api;
    public ThemeModel(IApiClient api) => _api = api;

    public class PreferencesModel
    {
        public string Theme { get; set; } = "light";
        public bool SidebarCollapsed { get; set; }
        public string Language { get; set; } = "en";
        public string? DashboardLayout { get; set; }
        public string? LandingPage { get; set; }
    }

    public async Task<JsonResult> OnGetCurrentAsync()
    {
        var result = await _api.GetAsync<PreferencesModel>("api/users/preferences");
        return new JsonResult(result);
    }

    public async Task<JsonResult> OnPostSaveAsync([FromBody] PreferencesModel model)
    {
        var result = await _api.PutAsync<PreferencesModel, object>("api/users/preferences", model);
        return new JsonResult(result);
    }
}
