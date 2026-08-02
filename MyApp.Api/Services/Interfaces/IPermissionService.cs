using MyApp.Api.Common;
using MyApp.Api.DTOs;
using MyApp.Api.Models;

namespace MyApp.Api.Services.Interfaces;

public interface IPermissionService
{
    Task<ApiResponse<List<PermissionType>>> GetTypesAsync();
    Task<ApiResponse<List<RolePermissionRow>>> GetMatrixAsync(int roleId);
    Task<ApiResponse<object>> AssignAsync(AssignPermissionsRequest request, int currentUserId);
    Task<ApiResponse<bool>> HasPermissionAsync(int userId, int roleId, string module, string action);
}
