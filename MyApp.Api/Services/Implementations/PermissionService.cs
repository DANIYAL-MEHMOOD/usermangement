using System.Data;
using Microsoft.Data.SqlClient;
using MyApp.Api.Common;
using MyApp.Api.Data;
using MyApp.Api.DTOs;
using MyApp.Api.Models;
using MyApp.Api.Services.Interfaces;

namespace MyApp.Api.Services.Implementations;

/// <summary>
/// Permission matrix: permission types, role matrix read/write, and the
/// per-cell user permission check used by authorization filters.
/// </summary>
public class PermissionService : IPermissionService
{
    private readonly SqlDataAccess _db;

    public PermissionService(SqlDataAccess db)
    {
        _db = db;
    }

    public async Task<ApiResponse<List<PermissionType>>> GetTypesAsync()
    {
        var types = await _db.ExecuteReaderAsync("dbo.sp_GetPermissionTypes", _ => { }, reader => new PermissionType
        {
            PermissionTypeId = reader.GetInt32(reader.GetOrdinal("PermissionTypeId")),
            Code = reader.GetString(reader.GetOrdinal("Code")),
            Name = reader.GetString(reader.GetOrdinal("Name"))
        });

        return ApiResponse<List<PermissionType>>.Ok(types);
    }

    public async Task<ApiResponse<List<RolePermissionRow>>> GetMatrixAsync(int roleId)
    {
        var matrix = await _db.ExecuteReaderAsync("dbo.sp_GetRolePermissions",
            cmd => SqlDataAccess.AddParam(cmd, "@RoleId", roleId),
            reader => new RolePermissionRow(
                reader.GetInt32(reader.GetOrdinal("MenuId")),
                reader.GetString(reader.GetOrdinal("MenuName")),
                reader.GetNullableInt32("ParentMenuId"),
                reader.GetInt32(reader.GetOrdinal("PermissionTypeId")),
                reader.GetString(reader.GetOrdinal("Code")),
                Convert.ToBoolean(reader["Granted"])));

        return ApiResponse<List<RolePermissionRow>>.Ok(matrix);
    }

    public async Task<ApiResponse<object>> AssignAsync(AssignPermissionsRequest request, int currentUserId)
    {
        await AssignPermissionsAsync(request.RoleId, request.Permissions, currentUserId);
        await LogAuditAsync(currentUserId, "Permissions", "Assign", null,
            $"Assigned {request.Permissions.Count} permissions to RoleId={request.RoleId}", null, null);

        return ApiResponse<object>.Ok(null, "Permissions updated successfully.");
    }

    public async Task<ApiResponse<bool>> HasPermissionAsync(int userId, int roleId, string module, string action)
    {
        var hasPermission = await _db.ExecuteScalarAsync<bool>("dbo.sp_UserHasPermission", cmd =>
        {
            SqlDataAccess.AddParam(cmd, "@UserId", userId);
            SqlDataAccess.AddParam(cmd, "@RoleId", roleId);
            SqlDataAccess.AddParam(cmd, "@ControllerPage", module);
            SqlDataAccess.AddParam(cmd, "@PermissionCode", action);
        });

        return ApiResponse<bool>.Ok(hasPermission ?? false);
    }

    // ------------------------------------------------------------------
    // Database access (stored procedures only)
    // ------------------------------------------------------------------

    private async Task AssignPermissionsAsync(int roleId, List<PermissionCellRequest> permissions, int userId)
    {
        var table = new DataTable();
        table.Columns.Add("MenuId", typeof(int));
        table.Columns.Add("PermissionTypeId", typeof(int));
        foreach (var p in permissions)
            table.Rows.Add(p.MenuId, p.PermissionTypeId);

        await using var connection = await _db.OpenConnectionAsync();
        await using var command = SqlDataAccess.CreateCommand(connection, "dbo.sp_AssignPermissions");

        SqlDataAccess.AddParam(command, "@RoleId", roleId);
        var tvp = command.Parameters.AddWithValue("@Permissions", table);
        tvp.SqlDbType = SqlDbType.Structured;
        tvp.TypeName = "dbo.MenuPermissionTableType";
        SqlDataAccess.AddParam(command, "@UserId", userId);

        await command.ExecuteNonQueryAsync();
    }

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
