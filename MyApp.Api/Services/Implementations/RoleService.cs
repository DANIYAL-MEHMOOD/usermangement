using MyApp.Api.Common;
using MyApp.Api.DTOs;
using MyApp.Api.Models;
using MyApp.Api.Repository.Interfaces;
using MyApp.Api.Services.Interfaces;

namespace MyApp.Api.Services.Implementations;

public class RoleService : IRoleService
{
    private readonly IRoleRepository _roles;
    private readonly IUserRepository _users;
    private readonly IAuditLogRepository _auditLog;

    public RoleService(IRoleRepository roles, IUserRepository users, IAuditLogRepository auditLog)
    {
        _roles = roles;
        _users = users;
        _auditLog = auditLog;
    }

    public async Task<ApiResponse<List<RoleItem>>> GetAllAsync(bool includeSystemRoles = true)
    {
        var items = await _roles.GetAllAsync(includeSystemRoles);
        return ApiResponse<List<RoleItem>>.Ok(items);
    }

    public async Task<ApiResponse<Role?>> GetByIdAsync(int id)
    {
        var role = await _roles.GetByIdAsync(id);
        if (role is null)
            return ApiResponse<Role?>.Fail("Role not found.");
        return ApiResponse<Role?>.Ok(role);
    }

    public async Task<ApiResponse<int>> SaveAsync(SaveRoleRequest request, int currentUserId)
    {
        if (request.RoleId.HasValue && request.RoleId.Value > 0)
        {
            var existing = await _roles.GetByIdAsync(request.RoleId.Value);
            if (existing is null)
                return ApiResponse<int>.Fail("Role not found.");
            if (existing.IsSystemRole)
                return ApiResponse<int>.Fail("System roles cannot be renamed or modified.");
        }

        var newId = await _roles.SaveAsync(request, currentUserId);
        await _auditLog.InsertAsync(currentUserId, "Roles", request.RoleId.HasValue ? "Update" : "Create",
            null, $"Role '{request.RoleName}' saved (ID={newId})", null, null);

        return ApiResponse<int>.Ok(newId, "Role saved successfully.");
    }

    public async Task<ApiResponse<object>> DeleteAsync(int id, int currentUserId)
    {
        var existing = await _roles.GetByIdAsync(id);
        if (existing is null)
            return ApiResponse<object>.Fail("Role not found.");
        if (existing.IsSystemRole)
            return ApiResponse<object>.Fail("System roles cannot be deleted.");

        await _roles.DeleteAsync(id, currentUserId);
        await _auditLog.InsertAsync(currentUserId, "Roles", "Delete", $"Role '{existing.RoleName}' (ID={id})", "Deleted", null, null);

        return ApiResponse<object>.Ok(null, "Role deleted successfully.");
    }

    public async Task<ApiResponse<int>> CloneAsync(CloneRoleRequest request, int currentUserId)
    {
        var source = await _roles.GetByIdAsync(request.SourceRoleId);
        if (source is null)
            return ApiResponse<int>.Fail("Source role not found.");

        var newId = await _roles.CloneAsync(request, currentUserId);
        await _auditLog.InsertAsync(currentUserId, "Roles", "Clone", $"Source RoleId={request.SourceRoleId}",
            $"Cloned to '{request.NewRoleName}' (ID={newId})", null, null);

        return ApiResponse<int>.Ok(newId, "Role cloned successfully.");
    }

    public async Task<ApiResponse<object>> AssignRoleAsync(int userId, int roleId, int currentUserId)
    {
        var user = await _users.GetByIdAsync(userId);
        if (user is null)
            return ApiResponse<object>.Fail("User not found.");

        var role = await _roles.GetByIdAsync(roleId);
        if (role is null)
            return ApiResponse<object>.Fail("Role not found.");

        await _users.AssignRoleAsync(userId, roleId, currentUserId);
        await _auditLog.InsertAsync(currentUserId, "Users", "AssignRole", $"UserId={userId}, OldRoleId={user.RoleId}",
            $"NewRoleId={roleId}", null, null);

        return ApiResponse<object>.Ok(null, "Role assigned successfully.");
    }
}
