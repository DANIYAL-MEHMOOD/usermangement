using MyApp.Web.Common;
using MyApp.Web.DTOs;

namespace MyApp.Web.Services.Interfaces;

public interface IMenuService
{
    Task<ApiResponse<List<MenuNodeDto>>?> GetAllAsync();
    Task<ApiResponse<List<MenuNodeDto>>?> GetUserMenusAsync();
    Task<ApiResponse<int>?> SaveAsync(SaveMenuRequestDto request);
    Task<ApiResponse<object>?> DeleteAsync(int id);
}

public interface IPermissionService
{
    Task<ApiResponse<List<PermissionTypeDto>>?> GetTypesAsync();
    Task<ApiResponse<List<RolePermissionRowDto>>?> GetMatrixAsync(int roleId);
    Task<ApiResponse<object>?> AssignAsync(AssignPermissionsRequestDto request);
    Task<bool> HasPermissionAsync(string module, string action);
}
