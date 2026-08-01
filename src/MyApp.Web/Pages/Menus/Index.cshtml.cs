using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MyApp.Web.Models;
using MyApp.Web.Services;

namespace MyApp.Web.Pages.Menus;

public class IndexModel : PageModel
{
    private readonly IApiClient _api;
    public IndexModel(IApiClient api) => _api = api;

    public async Task<JsonResult> OnGetAllAsync()
    {
        var result = await _api.GetAsync<List<MenuItem>>("api/menus");
        return new JsonResult(result);
    }

    public async Task<JsonResult> OnPostSaveAsync([FromBody] SaveMenuModel model)
    {
        var result = await _api.PostAsync<SaveMenuModel, object>("api/menus", model);
        return new JsonResult(result);
    }

    public async Task<JsonResult> OnPostDeleteAsync(int id)
    {
        var result = await _api.DeleteAsync($"api/menus/{id}");
        return new JsonResult(result);
    }
}
