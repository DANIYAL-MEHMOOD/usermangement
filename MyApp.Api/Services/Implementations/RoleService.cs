using System.Data;
using Microsoft.Data.SqlClient;
using MyApp.Api.Common;
using MyApp.Api.Data;
using MyApp.Api.DTOs;
using MyApp.Api.Models;
using MyApp.Api.Services.Interfaces;

namespace MyApp.Api.Services.Implementations;

/// <summary>
/// Role management: list, create/edit, delete, clone, and assign users to
/// roles. Database access via stored procedures and SqlDataAccess.
/// </summary>
public class RoleService : IRoleService
{
    private readonly SqlDataAccess _db;
    private readonly ILogger<RoleService> _logger;

    public RoleService(SqlDataAccess db, ILogger<RoleService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<ApiResponse<List<RoleItem>>> GetAllAsync(bool includeSystemRoles = true)
    {
        var items = await GetRolesAsync(null);
        if (!includeSystemRoles)
        {
            items = items.Where(r => !r.IsSystemRole).ToList();
        }
        return ApiResponse<List<RoleItem>>.Ok(items);
    }

    public async Task<ApiResponse<Role?>> GetByIdAsync(int id)
    {
        var role = await _db.ExecuteReaderSingleAsync("dbo.sp_GetRoleById",
            cmd => SqlDataAccess.AddParam(cmd, "@RoleId", id),
            MapRole);

        if (role is null)
            return ApiResponse<Role?>.Fail("Role not found.", 404);

        return ApiResponse<Role?>.Ok(role);
    }

    public async Task<ApiResponse<int>> SaveAsync(SaveRoleRequest request, int currentUserId)
    {
        if (string.IsNullOrWhiteSpace(request.RoleName))
            return ApiResponse<int>.Fail("Role name is required.");

        if (request.RoleId.HasValue && request.RoleId.Value > 0)
        {
            var existing = await _db.ExecuteReaderSingleAsync("dbo.sp_GetRoleById",
                cmd => SqlDataAccess.AddParam(cmd, "@RoleId", request.RoleId.Value),
                MapRole);

            if (existing is null)
                return ApiResponse<int>.Fail("Role not found.", 404);
            if (existing.IsSystemRole)
                return ApiResponse<int>.Fail("System roles cannot be renamed or modified.");
        }

        var newId = await SaveRoleAsync(request, currentUserId);
        await LogAuditAsync(currentUserId, "Roles", request.RoleId.HasValue ? "Update" : "Create",
            null, $"Role '{request.RoleName}' saved (ID={newId})", null, null);

        return ApiResponse<int>.Ok(newId, "Role saved successfully.");
    }

    public async Task<ApiResponse<object>> DeleteAsync(int id, int currentUserId)
    {
        var existing = await _db.ExecuteReaderSingleAsync("dbo.sp_GetRoleById",
            cmd => SqlDataAccess.AddParam(cmd, "@RoleId", id),
            MapRole);

        if (existing is null)
            return ApiResponse<object>.Fail("Role not found.", 404);
        if (existing.IsSystemRole)
            return ApiResponse<object>.Fail("System roles cannot be deleted.");

        await _db.ExecuteNonQueryAsync("dbo.sp_DeleteRole", cmd =>
        {
            SqlDataAccess.AddParam(cmd, "@RoleId", id);
            SqlDataAccess.AddParam(cmd, "@ModifiedBy", currentUserId);
        });

        await LogAuditAsync(currentUserId, "Roles", "Delete", $"Role '{existing.RoleName}' (ID={id})", "Deleted", null, null);

        return ApiResponse<object>.Ok(null, "Role deleted successfully.");
    }

    public async Task<ApiResponse<int>> CloneAsync(CloneRoleRequest request, int currentUserId)
    {
        var source = await _db.ExecuteReaderSingleAsync("dbo.sp_GetRoleById",
            cmd => SqlDataAccess.AddParam(cmd, "@RoleId", request.SourceRoleId),
            MapRole);

        if (source is null)
            return ApiResponse<int>.Fail("Source role not found.", 404);

        var newId = await CloneRoleAsync(request, currentUserId);
        await LogAuditAsync(currentUserId, "Roles", "Clone", $"Source RoleId={request.SourceRoleId}",
            $"Cloned to '{request.NewRoleName}' (ID={newId})", null, null);

        return ApiResponse<int>.Ok(newId, "Role cloned successfully.");
    }

    public async Task<ApiResponse<object>> AssignRoleAsync(int userId, int roleId, int currentUserId)
    {
        var user = await _db.ExecuteReaderSingleAsync("dbo.sp_GetUserById",
            cmd => SqlDataAccess.AddParam(cmd, "@UserId", userId),
            reader => (UserId: reader.GetInt32(reader.GetOrdinal("UserId")),
                       RoleId: reader.GetInt32(reader.GetOrdinal("RoleId"))));

        if (user is null)
            return ApiResponse<object>.Fail("User not found.", 404);

        var role = await _db.ExecuteReaderSingleAsync("dbo.sp_GetRoleById",
            cmd => SqlDataAccess.AddParam(cmd, "@RoleId", roleId),
            MapRole);

        if (role is null)
            return ApiResponse<object>.Fail("Role not found.", 404);

        await _db.ExecuteNonQueryAsync("dbo.sp_AssignRole", cmd =>
        {
            SqlDataAccess.AddParam(cmd, "@UserId", userId);
            SqlDataAccess.AddParam(cmd, "@RoleId", roleId);
            SqlDataAccess.AddParam(cmd, "@ModifiedBy", currentUserId);
        });

        await LogAuditAsync(currentUserId, "Users", "AssignRole", $"UserId={userId}, OldRoleId={user.Value.RoleId}",
            $"NewRoleId={roleId}", null, null);

        return ApiResponse<object>.Ok(null, "Role assigned successfully.");
    }

    // ------------------------------------------------------------------
    // Database access (stored procedures only)
    // ------------------------------------------------------------------

    private async Task<List<RoleItem>> GetRolesAsync(bool? isActive)
    {
        var roles = await _db.ExecuteReaderAsync("dbo.sp_GetRoles", cmd =>
        {
            SqlDataAccess.AddParam(cmd, "@SearchTerm", null);
            SqlDataAccess.AddParam(cmd, "@IsActive", isActive);
        }, reader => new Role
        {
            RoleId = reader.GetInt32(reader.GetOrdinal("RoleId")),
            RoleName = reader.GetString(reader.GetOrdinal("RoleName")),
            Description = reader.GetNullableString("Description"),
            IsSystemRole = reader.GetBoolean(reader.GetOrdinal("IsSystemRole")),
            IsActive = reader.GetBoolean(reader.GetOrdinal("IsActive")),
            CreatedDate = reader.GetDateTime(reader.GetOrdinal("CreatedDate")),
            UserCount = reader.GetInt32(reader.GetOrdinal("UserCount"))
        });

        return roles
            .OrderBy(r => r.RoleName)
            .Select(r => new RoleItem(r.RoleId, r.RoleName, r.Description, r.IsSystemRole, r.IsActive, r.UserCount))
            .ToList();
    }

    private async Task<int> SaveRoleAsync(SaveRoleRequest request, int userId)
    {
        await using var connection = await _db.OpenConnectionAsync();
        await using var command = SqlDataAccess.CreateCommand(connection, "dbo.sp_SaveRole");

        SqlDataAccess.AddParam(command, "@RoleId", request.RoleId);
        SqlDataAccess.AddParam(command, "@RoleName", request.RoleName);
        SqlDataAccess.AddParam(command, "@Description", request.Description);
        SqlDataAccess.AddParam(command, "@IsActive", request.IsActive);
        SqlDataAccess.AddParam(command, "@UserId", userId);
        var output = SqlDataAccess.AddOutputParam(command, "@NewRoleId", SqlDbType.Int);

        await command.ExecuteNonQueryAsync();
        return (int)output.Value;
    }

    private async Task<int> CloneRoleAsync(CloneRoleRequest request, int createdBy)
    {
        await using var connection = await _db.OpenConnectionAsync();
        await using var command = SqlDataAccess.CreateCommand(connection, "dbo.sp_CloneRole");

        SqlDataAccess.AddParam(command, "@SourceRoleId", request.SourceRoleId);
        SqlDataAccess.AddParam(command, "@NewRoleName", request.NewRoleName);
        SqlDataAccess.AddParam(command, "@Description", request.Description);
        SqlDataAccess.AddParam(command, "@CreatedBy", createdBy);
        var output = SqlDataAccess.AddOutputParam(command, "@NewRoleId", SqlDbType.Int);

        await command.ExecuteNonQueryAsync();
        return (int)output.Value;
    }

    private static Role MapRole(SqlDataReader reader) => new()
    {
        RoleId = reader.GetInt32(reader.GetOrdinal("RoleId")),
        RoleName = reader.GetString(reader.GetOrdinal("RoleName")),
        Description = reader.GetNullableString("Description"),
        IsSystemRole = reader.HasColumn("IsSystemRole") && reader.GetBoolean(reader.GetOrdinal("IsSystemRole")),
        IsActive = reader.HasColumn("IsActive") && reader.GetBoolean(reader.GetOrdinal("IsActive")),
        CreatedDate = reader.HasColumn("CreatedDate") ? reader.GetDateTime(reader.GetOrdinal("CreatedDate")) : default,
        UserCount = reader.HasColumn("UserCount") ? reader.GetInt32(reader.GetOrdinal("UserCount")) : 0
    };

    private async Task LogAuditAsync(int? userId, string module, string action, string? oldValue, string? newValue, string? browser, string? ipAddress) =>
        await _db.ExecuteNonQueryAsync("dbo.sp_InsertAuditLog", cmd =>
        {
            SqlDataAccess.AddParam(cmd, "@UserId", userId);
            SqlDataAccess.AddParam(cmd, "@Module", module);
            SqlDataAccess.AddParam(cmd, "@Action", action);
            SqlDataAccess.AddParam(cmd, "@OldValue", oldValue);
            SqlDataAccess.AddParam(cmd, "@NewValue", newValue);
            SqlDataAccess.AddParam(cmd, "@Browser", browser);
            SqlDataAccess.AddParam(cmd, "@IPAddress", ipAddress);
        });
}
