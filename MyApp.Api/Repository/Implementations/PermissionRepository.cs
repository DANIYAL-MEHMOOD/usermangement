using System.Data;
using Microsoft.Data.SqlClient;
using MyApp.Api.Repository.Interfaces;
using MyApp.Api.Security;
using MyApp.Api.Models;
using MyApp.Api.Data;

namespace MyApp.Api.Repository.Implementations;

public class PermissionRepository : IPermissionRepository
{
    private readonly SqlDataAccess _db;
    public PermissionRepository(SqlDataAccess db) => _db = db;

    public Task<List<PermissionType>> GetPermissionTypesAsync() =>
        _db.ExecuteReaderAsync("dbo.sp_GetPermissionTypes", _ => { }, reader => new PermissionType
        {
            PermissionTypeId = reader.GetInt32(reader.GetOrdinal("PermissionTypeId")),
            Code = reader.GetString(reader.GetOrdinal("Code")),
            Name = reader.GetString(reader.GetOrdinal("Name"))
        });

    public Task<List<(int MenuId, int? ParentMenuId, string MenuName, int MenuOrder, int PermissionTypeId, string Code, string Name, bool Granted)>>
        GetRolePermissionsAsync(int roleId) =>
        _db.ExecuteReaderAsync("dbo.sp_GetRolePermissions",
            cmd => SqlDataAccess.AddParam(cmd, "@RoleId", roleId),
            reader => (
                reader.GetInt32(reader.GetOrdinal("MenuId")),
                reader.IsDBNull(reader.GetOrdinal("ParentMenuId")) ? (int?)null : reader.GetInt32(reader.GetOrdinal("ParentMenuId")),
                reader.GetString(reader.GetOrdinal("MenuName")),
                reader.GetInt32(reader.GetOrdinal("MenuOrder")),
                reader.GetInt32(reader.GetOrdinal("PermissionTypeId")),
                reader.GetString(reader.GetOrdinal("Code")),
                reader.GetString(reader.GetOrdinal("Name")),
                Convert.ToBoolean(reader["Granted"])
            ));

    public async Task AssignPermissionsAsync(int roleId, List<(int MenuId, int PermissionTypeId)> permissions, int userId)
    {
        var table = new DataTable();
        table.Columns.Add("MenuId", typeof(int));
        table.Columns.Add("PermissionTypeId", typeof(int));
        foreach (var (menuId, permissionTypeId) in permissions)
            table.Rows.Add(menuId, permissionTypeId);

        await using var connection = await _db.OpenConnectionAsync();
        await using var command = SqlDataAccess.CreateCommand(connection, "dbo.sp_AssignPermissions");

        SqlDataAccess.AddParam(command, "@RoleId", roleId);
        var tvp = command.Parameters.AddWithValue("@Permissions", table);
        tvp.SqlDbType = SqlDbType.Structured;
        tvp.TypeName = "dbo.MenuPermissionTableType";
        SqlDataAccess.AddParam(command, "@UserId", userId);

        await command.ExecuteNonQueryAsync();
    }

    public async Task<bool> UserHasPermissionAsync(int userId, int roleId, string controllerPage, string permissionCode)
    {
        var result = await _db.ExecuteScalarAsync<bool>("dbo.sp_UserHasPermission", cmd =>
        {
            SqlDataAccess.AddParam(cmd, "@UserId", userId);
            SqlDataAccess.AddParam(cmd, "@RoleId", roleId);
            SqlDataAccess.AddParam(cmd, "@ControllerPage", controllerPage);
            SqlDataAccess.AddParam(cmd, "@PermissionCode", permissionCode);
        });
        return result;
    }
}
