using System.Data;
using Microsoft.Data.SqlClient;
using MyApp.Api.Data;
using MyApp.Api.Models;
using MyApp.Api.Repository.Interfaces;

namespace MyApp.Api.Repository.Implementations;

public class AuthRepository : IAuthRepository
{
    private readonly SqlDataAccess _db;

    public AuthRepository(SqlDataAccess db)
    {
        _db = db;
    }

    public Task<User?> GetForLoginAsync(string username) =>
        _db.ExecuteReaderSingleAsync("dbo.sp_Login",
            cmd => SqlDataAccess.AddParam(cmd, "@Username", username),
            MapUser);

    public Task RecordLoginSuccessAsync(int userId) =>
        _db.ExecuteNonQueryAsync("dbo.sp_RecordLoginSuccess",
            cmd => SqlDataAccess.AddParam(cmd, "@UserId", userId));

    public Task RecordLoginFailureAsync(int userId, int maxAttempts = 5, int lockoutMinutes = 15) =>
        _db.ExecuteNonQueryAsync("dbo.sp_RecordLoginFailure", cmd =>
        {
            SqlDataAccess.AddParam(cmd, "@UserId", userId);
            SqlDataAccess.AddParam(cmd, "@MaxAttempts", maxAttempts);
            SqlDataAccess.AddParam(cmd, "@LockoutMinutes", lockoutMinutes);
        });

    public Task UnlockExpiredLockoutsAsync() =>
        _db.ExecuteNonQueryAsync("dbo.sp_UnlockExpiredLockouts");

    public Task ChangePasswordAsync(int userId, byte[] newHash, byte[] newSalt, int expiryDays = 90) =>
        _db.ExecuteNonQueryAsync("dbo.sp_ChangePassword", cmd =>
        {
            SqlDataAccess.AddParam(cmd, "@UserId", userId);
            SqlDataAccess.AddParam(cmd, "@NewHash", newHash);
            SqlDataAccess.AddParam(cmd, "@NewSalt", newSalt);
            SqlDataAccess.AddParam(cmd, "@ExpiryDays", expiryDays);
        });

    public async Task<List<(byte[] Hash, byte[] Salt)>> GetPasswordHistoryAsync(int userId, int historyCount = 5)
    {
        return await _db.ExecuteReaderAsync("dbo.sp_CheckPasswordHistory",
            cmd =>
            {
                SqlDataAccess.AddParam(cmd, "@UserId", userId);
                SqlDataAccess.AddParam(cmd, "@HistoryCount", historyCount);
            },
            reader =>
            {
                var hash = (byte[])reader["PasswordHash"];
                var salt = (byte[])reader["PasswordSalt"];
                return (hash, salt);
            });
    }

    public async Task<int> CreatePasswordResetTokenAsync(int userId, string token, int expiryMinutes = 30)
    {
        await _db.ExecuteNonQueryAsync("dbo.sp_CreatePasswordResetToken", cmd =>
        {
            SqlDataAccess.AddParam(cmd, "@UserId", userId);
            SqlDataAccess.AddParam(cmd, "@Token", token);
            SqlDataAccess.AddParam(cmd, "@ExpiryMinutes", expiryMinutes);
        });
        return 1;
    }

    public Task<(int TokenId, int UserId, DateTime ExpiryDate, bool IsUsed)?> ValidatePasswordResetTokenAsync(string token) =>
        _db.ExecuteReaderSingleAsync("dbo.sp_ValidatePasswordResetToken",
            cmd => SqlDataAccess.AddParam(cmd, "@Token", token),
            reader => (
                reader.GetInt32(0),
                reader.GetInt32(1),
                reader.GetDateTime(2),
                reader.GetBoolean(3)
            ));

    public Task ConsumePasswordResetTokenAsync(int tokenId) =>
        _db.ExecuteNonQueryAsync("dbo.sp_ConsumePasswordResetToken",
            cmd => SqlDataAccess.AddParam(cmd, "@TokenId", tokenId));

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
            MapRefreshToken);

    public Task RevokeRefreshTokenAsync(string token, string? revokedByIp, string? replacedByToken = null) =>
        _db.ExecuteNonQueryAsync("dbo.sp_RevokeRefreshToken", cmd =>
        {
            SqlDataAccess.AddParam(cmd, "@Token", token);
            SqlDataAccess.AddParam(cmd, "@RevokedByIp", revokedByIp);
            SqlDataAccess.AddParam(cmd, "@ReplacedByToken", replacedByToken);
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

    private static RefreshToken MapRefreshToken(SqlDataReader reader) => new()
    {
        RefreshTokenId = reader.GetInt32(reader.GetOrdinal("RefreshTokenId")),
        UserId = reader.GetInt32(reader.GetOrdinal("UserId")),
        Token = reader.GetString(reader.GetOrdinal("Token")),
        ExpiryDate = reader.GetDateTime(reader.GetOrdinal("ExpiryDate")),
        CreatedDate = reader.GetDateTime(reader.GetOrdinal("CreatedDate")),
        CreatedByIp = reader.GetNullableString("CreatedByIp"),
        RevokedDate = reader.GetNullableDateTime("RevokedDate"),
        RevokedByIp = reader.GetNullableString("RevokedByIp"),
        ReplacedByToken = reader.GetNullableString("ReplacedByToken")
    };
}
