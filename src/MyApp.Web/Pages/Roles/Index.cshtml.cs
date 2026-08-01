using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MyApp.Web.Models;
using MyApp.Web.Services;

namespace MyApp.Web.Pages.Roles;

public class IndexModel : PageModel
{
    private readonly IApiClient _api;
    public IndexModel(IApiClient api) => _api = api;

    public async Task<JsonResult> OnGetSearchAsync(string? searchTerm, bool? isActive)
    {
        var query = new List<string>();
        if (!string.IsNullOrEmpty(searchTerm)) query.Add($"searchTerm={Uri.EscapeDataString(searchTerm)}");
        if (isActive is not null) query.Add($"isActive={isActive}");
        var qs = query.Count > 0 ? $"?{string.Join("&", query)}" : string.Empty;

        var result = await _api.GetAsync<List<RoleItem>>($"api/roles{qs}");
        return new JsonResult(result);
    }

    public async Task<JsonResult> OnPostSaveAsync([FromBody] SaveRoleModel model)
    {
        var result = await _api.PostAsync<SaveRoleModel, object>("api/roles", model);
        return new JsonResult(result);
    }

    public async Task<JsonResult> OnPostDeleteAsync(int id)
    {
        var result = await _api.DeleteAsync($"api/roles/{id}");
        return new JsonResult(result);
    }

    public async Task<JsonResult> OnPostCloneAsync([FromBody] CloneRoleModel model)
    {
        var result = await _api.PostAsync<CloneRoleModel, object>("api/roles/clone", model);
        return new JsonResult(result);
    }
}
