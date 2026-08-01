using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MyApp.Web.Models;
using MyApp.Web.Services;

namespace MyApp.Web.Pages.Permissions;

public class IndexModel : PageModel
{
    private readonly IApiClient _api;
    public IndexModel(IApiClient api) => _api = api;

    public List<RoleItem> Roles { get; set; } = [];

    [BindProperty(SupportsGet = true)]
    public int? RoleId { get; set; }

    public async Task OnGetAsync()
    {
        var result = await _api.GetAsync<List<RoleItem>>("api/roles");
        Roles = result is { Success: true, Data: not null } ? result.Data : [];
        RoleId ??= Roles.FirstOrDefault()?.RoleId;
    }

    public async Task<JsonResult> OnGetMatrixAsync(int roleId)
    {
        var result = await _api.GetAsync<List<PermissionMatrixCell>>($"api/permissions/matrix/{roleId}");
        return new JsonResult(result);
    }

    public async Task<JsonResult> OnPostAssignAsync([FromBody] AssignPermissionsModel model)
    {
        var result = await _api.PostAsync<AssignPermissionsModel, object>("api/permissions/assign", model);
        return new JsonResult(result);
    }
}
