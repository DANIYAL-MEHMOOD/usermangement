using System.Data;
using Microsoft.Data.SqlClient;
using MyApp.Api.Common;
using MyApp.Api.DTOs;
using MyApp.Api.Repository.Interfaces;
using MyApp.Api.Security;
using MyApp.Api.Models;
using MyApp.Api.Data;

namespace MyApp.Api.Repository.Implementations;

public class UserRepository : IUserRepository
{
    private readonly SqlDataAccess _db;

    public UserRepository(SqlDataAccess db)
    {
        _db = db;
    }

    public Task<User?> GetForLoginAsync(string username) =>
        _db.ExecuteReaderSingleAsync("dbo.sp_Login",
            cmd => SqlDataAccess.AddParam(cmd, "@Username", username),
            MapUser);

    public Task<User?> GetByIdAsync(int userId) =>
        _db.ExecuteReaderSingleAsync("dbo.sp_GetUserById",
            cmd => SqlDataAccess.AddParam(cmd, "@UserId", userId),
            MapUser);

    public async Task<PagedResult<UserListItemDto>> SearchAsync(UserSearchRequest request)
    {
        await using var connection = await _db.OpenConnectionAsync();
        await using var command = SqlDataAccess.CreateCommand(connection, "dbo.sp_GetUsers");

        SqlDataAccess.AddParam(command, "@SearchTerm", request.SearchTerm);
        SqlDataAccess.AddParam(command, "@RoleId", request.RoleId);
        SqlDataAccess.AddParam(command, "@Status", request.Status);
        SqlDataAccess.AddParam(command, "@Department", request.Department);
        SqlDataAccess.AddParam(command, "@SortColumn", request.SortColumn);
        SqlDataAccess.AddParam(command, "@SortDirection", request.SortDirection);
        SqlDataAccess.AddParam(command, "@PageNumber", request.PageNumber);
        SqlDataAccess.AddParam(command, "@PageSize", request.PageSize);

        var result = new PagedResult<UserListItemDto>();

        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            result.Items.Add(new UserListItemDto(
                reader.GetInt32(reader.GetOrdinal("UserId")),
                reader.IsDBNull(reader.GetOrdinal("EmployeeNumber")) ? null : reader.GetString(reader.GetOrdinal("EmployeeNumber")),
                reader.GetString(reader.GetOrdinal("Username")),
                reader.GetString(reader.GetOrdinal("FullName")),
                reader.GetString(reader.GetOrdinal("Email")),
                reader.IsDBNull(reader.GetOrdinal("Phone")) ? null : reader.GetString(reader.GetOrdinal("Phone")),
                reader.IsDBNull(reader.GetOrdinal("Designation")) ? null : reader.GetString(reader.GetOrdinal("Designation")),
                reader.IsDBNull(reader.GetOrdinal("Department")) ? null : reader.GetString(reader.GetOrdinal("Department")),
                reader.IsDBNull(reader.GetOrdinal("ProfilePicturePath")) ? null : reader.GetString(reader.GetOrdinal("ProfilePicturePath")),
                reader.GetInt32(reader.GetOrdinal("RoleId")),
                reader.GetString(reader.GetOrdinal("RoleName")),
                reader.GetByte(reader.GetOrdinal("Status")),
                reader.IsDBNull(reader.GetOrdinal("LastLogin")) ? null : reader.GetDateTime(reader.GetOrdinal("LastLogin")),
                reader.GetInt32(reader.GetOrdinal("LoginCount")),
                reader.GetDateTime(reader.GetOrdinal("CreatedDate"))));
        }

        if (await reader.NextResultAsync() && await reader.ReadAsync())
        {
            result.TotalCount = reader.GetInt32(0);
        }

        return result;
    }

    public async Task<int> CreateAsync(CreateUserRequest request, byte[] hash, byte[] salt, int createdBy)
    {
        await using var connection = await _db.OpenConnectionAsync();
        await using var command = SqlDataAccess.CreateCommand(connection, "dbo.sp_CreateUser");

        SqlDataAccess.AddParam(command, "@EmployeeNumber", request.EmployeeNumber);
        SqlDataAccess.AddParam(command, "@Username", request.Username);
        SqlDataAccess.AddParam(command, "@PasswordHash", hash, SqlDbType.VarBinary);
        SqlDataAccess.AddParam(command, "@PasswordSalt", salt, SqlDbType.VarBinary);
        SqlDataAccess.AddParam(command, "@FullName", request.FullName);
        SqlDataAccess.AddParam(command, "@Email", request.Email);
        SqlDataAccess.AddParam(command, "@Phone", request.Phone);
        SqlDataAccess.AddParam(command, "@Designation", request.Designation);
        SqlDataAccess.AddParam(command, "@Department", request.Department);
        SqlDataAccess.AddParam(command, "@ProfilePicturePath", null);
        SqlDataAccess.AddParam(command, "@RoleId", request.RoleId);
        SqlDataAccess.AddParam(command, "@CreatedBy", createdBy);
        var output = SqlDataAccess.AddOutputParam(command, "@NewUserId", SqlDbType.Int);

        await command.ExecuteNonQueryAsync();
        return (int)output.Value;
    }

    public Task UpdateAsync(int userId, UpdateUserRequest request, int modifiedBy) =>
        _db.ExecuteNonQueryAsync("dbo.sp_UpdateUser", cmd =>
        {
            SqlDataAccess.AddParam(cmd, "@UserId", userId);
            SqlDataAccess.AddParam(cmd, "@EmployeeNumber", request.EmployeeNumber);
            SqlDataAccess.AddParam(cmd, "@FullName", request.FullName);
            SqlDataAccess.AddParam(cmd, "@Email", request.Email);
            SqlDataAccess.AddParam(cmd, "@Phone", request.Phone);
            SqlDataAccess.AddParam(cmd, "@Designation", request.Designation);
            SqlDataAccess.AddParam(cmd, "@Department", request.Department);
            SqlDataAccess.AddParam(cmd, "@ProfilePicturePath", null);
            SqlDataAccess.AddParam(cmd, "@RoleId", request.RoleId);
            SqlDataAccess.AddParam(cmd, "@ModifiedBy", modifiedBy);
        });

    public Task DeleteAsync(int userId, int modifiedBy) =>
        _db.ExecuteNonQueryAsync("dbo.sp_DeleteUser", cmd =>
        {
            SqlDataAccess.AddParam(cmd, "@UserId", userId);
            SqlDataAccess.AddParam(cmd, "@ModifiedBy", modifiedBy);
        });

    public Task SetStatusAsync(int userId, byte status, int modifiedBy) =>
        _db.ExecuteNonQueryAsync("dbo.sp_SetUserStatus", cmd =>
        {
            SqlDataAccess.AddParam(cmd, "@UserId", userId);
            SqlDataAccess.AddParam(cmd, "@Status", status, SqlDbType.TinyInt);
            SqlDataAccess.AddParam(cmd, "@ModifiedBy", modifiedBy);
        });

    public Task AssignRoleAsync(int userId, int roleId, int modifiedBy) =>
        _db.ExecuteNonQueryAsync("dbo.sp_AssignRole", cmd =>
        {
            SqlDataAccess.AddParam(cmd, "@UserId", userId);
            SqlDataAccess.AddParam(cmd, "@RoleId", roleId);
            SqlDataAccess.AddParam(cmd, "@ModifiedBy", modifiedBy);
        });

    public Task RecordLoginSuccessAsync(int userId) =>
        _db.ExecuteNonQueryAsync("dbo.sp_RecordLoginSuccess", cmd => SqlDataAccess.AddParam(cmd, "@UserId", userId));

    public Task RecordLoginFailureAsync(int userId, int maxAttempts = 5, int lockoutMinutes = 15) =>
        _db.ExecuteNonQueryAsync("dbo.sp_RecordLoginFailure", cmd =>
        {
            SqlDataAccess.AddParam(cmd, "@UserId", userId);
            SqlDataAccess.AddParam(cmd, "@MaxAttempts", maxAttempts);
            SqlDataAccess.AddParam(cmd, "@LockoutMinutes", lockoutMinutes);
        });

    public Task UnlockExpiredLockoutsAsync() =>
        _db.ExecuteNonQueryAsync("dbo.sp_UnlockExpiredLockouts", _ => { });

    public Task ChangePasswordAsync(int userId, byte[] newHash, byte[] newSalt, int expiryDays = 90) =>
        _db.ExecuteNonQueryAsync("dbo.sp_ChangePassword", cmd =>
        {
            SqlDataAccess.AddParam(cmd, "@UserId", userId);
            SqlDataAccess.AddParam(cmd, "@NewPasswordHash", newHash, SqlDbType.VarBinary);
            SqlDataAccess.AddParam(cmd, "@NewPasswordSalt", newSalt, SqlDbType.VarBinary);
            SqlDataAccess.AddParam(cmd, "@ExpiryDays", expiryDays);
        });

    public Task<List<(byte[] Hash, byte[] Salt)>> GetPasswordHistoryAsync(int userId, int historyCount = 5) =>
        _db.ExecuteReaderAsync("dbo.sp_CheckPasswordHistory", cmd =>
        {
            SqlDataAccess.AddParam(cmd, "@UserId", userId);
            SqlDataAccess.AddParam(cmd, "@HistoryCount", historyCount);
        }, reader => ((byte[])reader["PasswordHash"], (byte[])reader["PasswordSalt"]));

    public async Task<int> CreatePasswordResetTokenAsync(int userId, string token, int expiryMinutes = 30)
    {
        await _db.ExecuteNonQueryAsync("dbo.sp_CreatePasswordResetToken", cmd =>
        {
            SqlDataAccess.AddParam(cmd, "@UserId", userId);
            SqlDataAccess.AddParam(cmd, "@Token", token);
            SqlDataAccess.AddParam(cmd, "@ExpiryMinutes", expiryMinutes);
        });
        return userId;
    }

    public Task<(int TokenId, int UserId, DateTime ExpiryDate, bool IsUsed)?> ValidatePasswordResetTokenAsync(string token) =>
        _db.ExecuteReaderSingleAsync<(int, int, DateTime, bool)?>("dbo.sp_ValidatePasswordResetToken",
            cmd => SqlDataAccess.AddParam(cmd, "@Token", token),
            reader => (reader.GetInt32(0), reader.GetInt32(1), reader.GetDateTime(2), reader.GetBoolean(3)));

    public Task ConsumePasswordResetTokenAsync(int tokenId) =>
        _db.ExecuteNonQueryAsync("dbo.sp_ConsumePasswordResetToken", cmd => SqlDataAccess.AddParam(cmd, "@TokenId", tokenId));

    public Task SaveRefreshTokenAsync(int userId, string token, DateTime expiryDate, string? createdByIp) =>
        _db.ExecuteNonQueryAsync("dbo.sp_SaveRefreshToken", cmd =>
        {
            SqlDataAccess.AddParam(cmd, "@UserId", userId);
            SqlDataAccess.AddParam(cmd, "@Token", token);
            SqlDataAccess.AddParam(cmd, "@ExpiryDate", expiryDate);
            SqlDataAccess.AddParam(cmd, "@CreatedByIp", createdByIp);
        });

    public Task<RefreshToken?> GetRefreshTokenAsync(string token) =>
        _db.ExecuteReaderSingleAsync("dbo.sp_GetRefreshToken",
            cmd => SqlDataAccess.AddParam(cmd, "@Token", token),
            reader => new RefreshToken
            {
                RefreshTokenId = reader.GetInt32(reader.GetOrdinal("RefreshTokenId")),
                UserId = reader.GetInt32(reader.GetOrdinal("UserId")),
                Token = reader.GetString(reader.GetOrdinal("Token")),
                ExpiryDate = reader.GetDateTime(reader.GetOrdinal("ExpiryDate")),
                RevokedDate = reader.IsDBNull(reader.GetOrdinal("RevokedDate")) ? null : reader.GetDateTime(reader.GetOrdinal("RevokedDate")),
                ReplacedByToken = reader.IsDBNull(reader.GetOrdinal("ReplacedByToken")) ? null : reader.GetString(reader.GetOrdinal("ReplacedByToken"))
            });

    public Task RevokeRefreshTokenAsync(string token, string? revokedByIp, string? replacedByToken = null) =>
        _db.ExecuteNonQueryAsync("dbo.sp_RevokeRefreshToken", cmd =>
        {
            SqlDataAccess.AddParam(cmd, "@Token", token);
            SqlDataAccess.AddParam(cmd, "@RevokedByIp", revokedByIp);
            SqlDataAccess.AddParam(cmd, "@ReplacedByToken", replacedByToken);
        });

    public Task<UserPreferences?> GetPreferencesAsync(int userId) =>
        _db.ExecuteReaderSingleAsync("dbo.sp_GetUserPreferences",
            cmd => SqlDataAccess.AddParam(cmd, "@UserId", userId),
            reader => new UserPreferences
            {
                UserId = reader.GetInt32(reader.GetOrdinal("UserId")),
                Theme = reader.GetString(reader.GetOrdinal("Theme")),
                SidebarCollapsed = reader.GetBoolean(reader.GetOrdinal("SidebarCollapsed")),
                Language = reader.GetString(reader.GetOrdinal("Language")),
                DashboardLayout = reader.IsDBNull(reader.GetOrdinal("DashboardLayout")) ? null : reader.GetString(reader.GetOrdinal("DashboardLayout")),
                LandingPage = reader.IsDBNull(reader.GetOrdinal("LandingPage")) ? null : reader.GetString(reader.GetOrdinal("LandingPage"))
            });

    public Task SavePreferencesAsync(int userId, UserPreferences preferences) =>
        _db.ExecuteNonQueryAsync("dbo.sp_SaveUserPreferences", cmd =>
        {
            SqlDataAccess.AddParam(cmd, "@UserId", userId);
            SqlDataAccess.AddParam(cmd, "@Theme", preferences.Theme);
            SqlDataAccess.AddParam(cmd, "@SidebarCollapsed", preferences.SidebarCollapsed);
            SqlDataAccess.AddParam(cmd, "@Language", preferences.Language);
            SqlDataAccess.AddParam(cmd, "@DashboardLayout", preferences.DashboardLayout);
            SqlDataAccess.AddParam(cmd, "@LandingPage", preferences.LandingPage);
        });

    private static User MapUser(SqlDataReader reader) => new()
    {
        UserId = reader.GetInt32(reader.GetOrdinal("UserId")),
        Username = reader.GetString(reader.GetOrdinal("Username")),
        PasswordHash = reader.HasColumn("PasswordHash") && !reader.IsDBNull(reader.GetOrdinal("PasswordHash"))
            ? (byte[])reader["PasswordHash"] : [],
        PasswordSalt = reader.HasColumn("PasswordSalt") && !reader.IsDBNull(reader.GetOrdinal("PasswordSalt"))
            ? (byte[])reader["PasswordSalt"] : [],
        FullName = reader.GetString(reader.GetOrdinal("FullName")),
        Email = reader.HasColumn("Email") ? reader.GetString(reader.GetOrdinal("Email")) : string.Empty,
        RoleId = reader.GetInt32(reader.GetOrdinal("RoleId")),
        RoleName = reader.GetString(reader.GetOrdinal("RoleName")),
        Status = reader.HasColumn("Status") ? Convert.ToByte(reader["Status"]) : (byte)0,
        IsLocked = reader.HasColumn("IsLocked") && reader.GetBoolean(reader.GetOrdinal("IsLocked")),
        FailedLoginAttempts = reader.HasColumn("FailedLoginAttempts") ? reader.GetInt32(reader.GetOrdinal("FailedLoginAttempts")) : 0,
        PasswordExpiryDate = reader.HasColumn("PasswordExpiryDate") && !reader.IsDBNull(reader.GetOrdinal("PasswordExpiryDate"))
            ? reader.GetDateTime(reader.GetOrdinal("PasswordExpiryDate")) : null,
        MustChangePassword = reader.HasColumn("MustChangePassword") && reader.GetBoolean(reader.GetOrdinal("MustChangePassword")),
        LastLogin = reader.HasColumn("LastLogin") && !reader.IsDBNull(reader.GetOrdinal("LastLogin"))
            ? reader.GetDateTime(reader.GetOrdinal("LastLogin")) : null,
        LoginCount = reader.HasColumn("LoginCount") ? reader.GetInt32(reader.GetOrdinal("LoginCount")) : 0,
        CreatedDate = reader.HasColumn("CreatedDate") ? reader.GetDateTime(reader.GetOrdinal("CreatedDate")) : default,
        ModifiedDate = reader.HasColumn("ModifiedDate") && !reader.IsDBNull(reader.GetOrdinal("ModifiedDate"))
            ? reader.GetDateTime(reader.GetOrdinal("ModifiedDate")) : null
    };
}
