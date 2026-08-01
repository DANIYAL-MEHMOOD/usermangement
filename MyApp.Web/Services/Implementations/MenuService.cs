using MyApp.Web.ApiClient;
using MyApp.Web.Common;
using MyApp.Web.DTOs;
using MyApp.Web.Services.Interfaces;

namespace MyApp.Web.Services.Implementations;

public class MenuService : IMenuService
{
    private readonly IApiClient _api;

    public MenuService(IApiClient api)
    {
        _api = api;
    }

    public Task<ApiResponse<List<MenuNodeDto>>?> GetAllAsync() =>
        _api.GetAsync<List<MenuNodeDto>>("api/menus");

    public Task<ApiResponse<List<MenuNodeDto>>?> GetUserMenusAsync() =>
        _api.GetAsync<List<MenuNodeDto>>("api/menus/mine");

    public Task<ApiResponse<int>?> SaveAsync(SaveMenuRequestDto request) =>
        _api.PostAsync<SaveMenuRequestDto, int>("api/menus", request);

    public Task<ApiResponse<object>?> DeleteAsync(int id) =>
        _api.DeleteAsync($"api/menus/{id}");
}

public class PermissionService : IPermissionService
{
    private readonly IApiClient _api;

    public PermissionService(IApiClient api)
    {
        _api = api;
    }

    public Task<ApiResponse<List<PermissionTypeDto>>?> GetTypesAsync() =>
        _api.GetAsync<List<PermissionTypeDto>>("api/permissions/types");

    public Task<ApiResponse<List<RolePermissionRowDto>>?> GetMatrixAsync(int roleId) =>
        _api.GetAsync<List<RolePermissionRowDto>>($"api/permissions/matrix/{roleId}");

    public Task<ApiResponse<object>?> AssignAsync(AssignPermissionsRequestDto request) =>
        _api.PostAsync<AssignPermissionsRequestDto, object>("api/permissions/assign", request);

    public async Task<bool> HasPermissionAsync(string module, string action)
    {
        var response = await _api.GetAsync<bool>($"api/permissions/check?module={Uri.EscapeDataString(module)}&action={Uri.EscapeDataString(action)}");
        return response is { Success: true, Data: true };
    }
}
