using MyApp.Api.Common;
using MyApp.Api.DTOs;
using MyApp.Api.Models;
using MyApp.Api.Repository.Interfaces;
using MyApp.Api.Services.Interfaces;

namespace MyApp.Api.Services.Implementations;

public class PermissionService : IPermissionService
{
    private readonly IPermissionRepository _permissions;
    private readonly IAuditLogRepository _auditLog;

    public PermissionService(IPermissionRepository permissions, IAuditLogRepository auditLog)
    {
        _permissions = permissions;
        _auditLog = auditLog;
    }

    public async Task<ApiResponse<List<PermissionType>>> GetTypesAsync()
    {
        var types = await _permissions.GetTypesAsync();
        return ApiResponse<List<PermissionType>>.Ok(types);
    }

    public async Task<ApiResponse<List<RolePermissionRow>>> GetMatrixAsync(int roleId)
    {
        var matrix = await _permissions.GetRolePermissionsAsync(roleId);
        return ApiResponse<List<RolePermissionRow>>.Ok(matrix);
    }

    public async Task<ApiResponse<object>> AssignAsync(AssignPermissionsRequest request, int currentUserId)
    {
        await _permissions.AssignAsync(request.RoleId, request.Permissions, currentUserId);
        await _auditLog.InsertAsync(currentUserId, "Permissions", "Assign", null,
            $"Assigned {request.Permissions.Count} permissions to RoleId={request.RoleId}", null, null);

        return ApiResponse<object>.Ok(null, "Permissions updated successfully.");
    }

    public async Task<ApiResponse<bool>> HasPermissionAsync(int userId, string module, string action)
    {
        var hasPerm = await _permissions.HasPermissionAsync(userId, module, action);
        return ApiResponse<bool>.Ok(hasPerm);
    }
}
