using MyApp.Web.ApiClient;
using MyApp.Web.Common;
using MyApp.Web.DTOs;
using MyApp.Web.Services.Interfaces;

namespace MyApp.Web.Services.Implementations;

public class UserService : IUserService
{
    private readonly IApiClient _api;

    public UserService(IApiClient api)
    {
        _api = api;
    }

    public Task<ApiResponse<List<UserListItemDto>>?> SearchAsync(UserSearchRequestDto request)
    {
        var query = $"api/users?SearchTerm={Uri.EscapeDataString(request.SearchTerm ?? "")}&RoleId={request.RoleId}&Status={request.Status}&Department={Uri.EscapeDataString(request.Department ?? "")}&SortColumn={request.SortColumn}&SortDirection={request.SortDirection}&PageNumber={request.PageNumber}&PageSize={request.PageSize}";
        return _api.GetAsync<List<UserListItemDto>>(query);
    }

    public Task<ApiResponse<UserDetailDto?>?> GetByIdAsync(int id) =>
        _api.GetAsync<UserDetailDto?>($"api/users/{id}");

    public Task<ApiResponse<int>?> CreateAsync(CreateUserRequestDto request) =>
        _api.PostAsync<CreateUserRequestDto, int>("api/users", request);

    public Task<ApiResponse<object>?> UpdateAsync(int id, UpdateUserRequestDto request) =>
        _api.PutAsync<UpdateUserRequestDto, object>($"api/users/{id}", request);

    public Task<ApiResponse<object>?> DeleteAsync(int id) =>
        _api.DeleteAsync($"api/users/{id}");

    public Task<ApiResponse<object>?> ActivateAsync(int id) =>
        _api.PatchAsync<object>($"api/users/{id}/activate");

    public Task<ApiResponse<object>?> DeactivateAsync(int id) =>
        _api.PatchAsync<object>($"api/users/{id}/deactivate");
}

public class RoleService : IRoleService
{
    private readonly IApiClient _api;

    public RoleService(IApiClient api)
    {
        _api = api;
    }

    public Task<ApiResponse<List<RoleItemDto>>?> GetAllAsync(bool includeSystem = true) =>
        _api.GetAsync<List<RoleItemDto>>($"api/roles?includeSystem={includeSystem}");

    public Task<ApiResponse<RoleItemDto?>?> GetByIdAsync(int id) =>
        _api.GetAsync<RoleItemDto?>($"api/roles/{id}");

    public Task<ApiResponse<int>?> SaveAsync(SaveRoleRequestDto request) =>
        _api.PostAsync<SaveRoleRequestDto, int>("api/roles", request);

    public Task<ApiResponse<object>?> DeleteAsync(int id) =>
        _api.DeleteAsync($"api/roles/{id}");

    public Task<ApiResponse<int>?> CloneAsync(CloneRoleRequestDto request) =>
        _api.PostAsync<CloneRoleRequestDto, int>("api/roles/clone", request);

    public Task<ApiResponse<object>?> AssignRoleAsync(int userId, int roleId) =>
        _api.PatchAsync<object>($"api/roles/{userId}/assign/{roleId}");
}
