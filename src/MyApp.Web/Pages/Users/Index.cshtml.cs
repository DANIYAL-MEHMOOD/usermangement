using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MyApp.Web.Models;
using MyApp.Web.Services;

namespace MyApp.Web.Pages.Users;

public class IndexModel : PageModel
{
    private readonly IApiClient _api;
    public IndexModel(IApiClient api) => _api = api;

    public List<RoleItem> Roles { get; set; } = [];
    public bool IsAdministrator => string.Equals(
        HttpContext.Session.GetString(SessionKeys.RoleName), "Administrator", StringComparison.OrdinalIgnoreCase);

    public async Task OnGetAsync()
    {
        // Roles are needed for the Create/Edit form's role dropdown.
        var result = await _api.GetAsync<List<RoleItem>>("api/roles");
        Roles = result is { Success: true, Data: not null } ? result.Data : [];
    }

    /// <summary>AJAX search/filter/sort/paginate — called from the page's JS on
    /// load and on every filter change, so the whole table never causes a full
    /// page reload.</summary>
    public async Task<JsonResult> OnGetSearchAsync(
        string? searchTerm, int? roleId, byte? status, string? department,
        string sortColumn = "FullName", string sortDirection = "ASC", int pageNumber = 1, int pageSize = 25)
    {
        var query = BuildQuery(new Dictionary<string, string?>
        {
            ["searchTerm"] = searchTerm,
            ["roleId"] = roleId?.ToString(),
            ["status"] = status?.ToString(),
            ["department"] = department,
            ["sortColumn"] = sortColumn,
            ["sortDirection"] = sortDirection,
            ["pageNumber"] = pageNumber.ToString(),
            ["pageSize"] = pageSize.ToString()
        });

        var result = await _api.GetAsync<List<UserListItem>>($"api/users{query}");
        return new JsonResult(result);
    }

    public async Task<JsonResult> OnPostCreateAsync([FromBody] CreateUserModel model)
    {
        var result = await _api.PostAsync<CreateUserModel, object>("api/users", model);
        return new JsonResult(result);
    }

    public async Task<JsonResult> OnPostUpdateAsync(int id, [FromBody] UpdateUserModel model)
    {
        var result = await _api.PutAsync<UpdateUserModel, object>($"api/users/{id}", model);
        return new JsonResult(result);
    }

    public async Task<JsonResult> OnPostDeleteAsync(int id)
    {
        var result = await _api.DeleteAsync($"api/users/{id}");
        return new JsonResult(result);
    }

    public async Task<JsonResult> OnPostActivateAsync(int id)
    {
        var result = await _api.PatchAsync<object>($"api/users/{id}/activate");
        return new JsonResult(result);
    }

    public async Task<JsonResult> OnPostDeactivateAsync(int id)
    {
        var result = await _api.PatchAsync<object>($"api/users/{id}/deactivate");
        return new JsonResult(result);
    }

    private static string BuildQuery(Dictionary<string, string?> parameters)
    {
        var parts = parameters
            .Where(p => !string.IsNullOrEmpty(p.Value))
            .Select(p => $"{Uri.EscapeDataString(p.Key)}={Uri.EscapeDataString(p.Value!)}");
        var joined = string.Join("&", parts);
        return string.IsNullOrEmpty(joined) ? string.Empty : $"?{joined}";
    }
}
