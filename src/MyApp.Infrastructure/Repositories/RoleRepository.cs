using System.Data;
using MyApp.Application.Interfaces;
using MyApp.Domain.Entities;
using MyApp.Infrastructure.Data;

namespace MyApp.Infrastructure.Repositories;

public class RoleRepository : IRoleRepository
{
    private readonly SqlDataAccess _db;
    public RoleRepository(SqlDataAccess db) => _db = db;

    public Task<List<Role>> SearchAsync(string? searchTerm, bool? isActive) =>
        _db.ExecuteReaderAsync("dbo.sp_GetRoles", cmd =>
        {
            SqlDataAccess.AddParam(cmd, "@SearchTerm", searchTerm);
            SqlDataAccess.AddParam(cmd, "@IsActive", isActive);
        }, reader => new Role
        {
            RoleId = reader.GetInt32(reader.GetOrdinal("RoleId")),
            RoleName = reader.GetString(reader.GetOrdinal("RoleName")),
            Description = reader.IsDBNull(reader.GetOrdinal("Description")) ? null : reader.GetString(reader.GetOrdinal("Description")),
            IsSystemRole = reader.GetBoolean(reader.GetOrdinal("IsSystemRole")),
            IsActive = reader.GetBoolean(reader.GetOrdinal("IsActive")),
            CreatedDate = reader.GetDateTime(reader.GetOrdinal("CreatedDate")),
            UserCount = reader.GetInt32(reader.GetOrdinal("UserCount"))
        });

    public Task<Role?> GetByIdAsync(int roleId) =>
        _db.ExecuteReaderSingleAsync("dbo.sp_GetRoleById",
            cmd => SqlDataAccess.AddParam(cmd, "@RoleId", roleId),
            reader => new Role
            {
                RoleId = reader.GetInt32(reader.GetOrdinal("RoleId")),
                RoleName = reader.GetString(reader.GetOrdinal("RoleName")),
                Description = reader.IsDBNull(reader.GetOrdinal("Description")) ? null : reader.GetString(reader.GetOrdinal("Description")),
                IsSystemRole = reader.GetBoolean(reader.GetOrdinal("IsSystemRole")),
                IsActive = reader.GetBoolean(reader.GetOrdinal("IsActive")),
                CreatedDate = reader.GetDateTime(reader.GetOrdinal("CreatedDate"))
            });

    public async Task<int> SaveAsync(int? roleId, string roleName, string? description, bool isActive, int userId)
    {
        await using var connection = await _db.OpenConnectionAsync();
        await using var command = SqlDataAccess.CreateCommand(connection, "dbo.sp_SaveRole");

        SqlDataAccess.AddParam(command, "@RoleId", roleId);
        SqlDataAccess.AddParam(command, "@RoleName", roleName);
        SqlDataAccess.AddParam(command, "@Description", description);
        SqlDataAccess.AddParam(command, "@IsActive", isActive);
        SqlDataAccess.AddParam(command, "@UserId", userId);
        var output = SqlDataAccess.AddOutputParam(command, "@NewRoleId", SqlDbType.Int);

        await command.ExecuteNonQueryAsync();
        return (int)output.Value;
    }

    public Task DeleteAsync(int roleId, int modifiedBy) =>
        _db.ExecuteNonQueryAsync("dbo.sp_DeleteRole", cmd =>
        {
            SqlDataAccess.AddParam(cmd, "@RoleId", roleId);
            SqlDataAccess.AddParam(cmd, "@ModifiedBy", modifiedBy);
        });

    public async Task<int> CloneAsync(int sourceRoleId, string newRoleName, string? description, int createdBy)
    {
        await using var connection = await _db.OpenConnectionAsync();
        await using var command = SqlDataAccess.CreateCommand(connection, "dbo.sp_CloneRole");

        SqlDataAccess.AddParam(command, "@SourceRoleId", sourceRoleId);
        SqlDataAccess.AddParam(command, "@NewRoleName", newRoleName);
        SqlDataAccess.AddParam(command, "@Description", description);
        SqlDataAccess.AddParam(command, "@CreatedBy", createdBy);
        var output = SqlDataAccess.AddOutputParam(command, "@NewRoleId", SqlDbType.Int);

        await command.ExecuteNonQueryAsync();
        return (int)output.Value;
    }
}
