using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MyApp.Web.Services;

namespace MyApp.Web.Pages;

public class DashboardModel : PageModel
{
    private readonly IApiClient _api;
    public DashboardModel(IApiClient api) => _api = api;

    public void OnGet()
    {
        // AuthGuardFilter (Program.cs) already redirects to /Login if the
        // session has no token, so no manual check is needed here.
    }

    public async Task<JsonResult> OnGetDataAsync()
    {
        var result = await _api.GetAsync<object>("api/dashboard");
        return new JsonResult(result);
    }
}
