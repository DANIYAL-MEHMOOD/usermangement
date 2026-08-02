using System.Data;
using Microsoft.Data.SqlClient;
using MyApp.Api.Common;
using MyApp.Api.Data;
using MyApp.Api.DTOs;
using MyApp.Api.Helpers;
using MyApp.Api.Models;
using MyApp.Api.Security;
using MyApp.Api.Services.Interfaces;

namespace MyApp.Api.Services.Implementations;

/// <summary>
/// User management: search/filter/sort/pagination, CRUD, activate/deactivate,
/// role assignment and preferences. Database access via stored procedures and
/// the centralized <see cref="SqlDataAccess"/> component.
/// </summary>
public class UserService : IUserService
{
    private readonly SqlDataAccess _db;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ILogger<UserService> _logger;

    public UserService(SqlDataAccess db, IPasswordHasher passwordHasher, ILogger<UserService> logger)
    {
        _db = db;
        _passwordHasher = passwordHasher;
        _logger = logger;
    }

    public async Task<ApiResponse<List<UserListItemDto>>> SearchAsync(UserSearchRequest request)
    {
        var result = await SearchUsersAsync(request);
        var pagination = new PaginationMeta
        {
            PageNumber = request.PageNumber,
            PageSize = request.PageSize,
            TotalCount = result.TotalCount
        };
        return ApiResponse<List<UserListItemDto>>.Ok(result.Items, pagination: pagination);
    }

    public async Task<ApiResponse<UserDetailDto?>> GetByIdAsync(int id)
    {
        var user = await GetDetailAsync(id);
        if (user is null)
            return ApiResponse<UserDetailDto?>.Fail("User not found.", 404);

        return ApiResponse<UserDetailDto?>.Ok(user);
    }

    public async Task<ApiResponse<int>> CreateAsync(CreateUserRequest request, int currentUserId)
    {
        if (request.Password != request.ConfirmPassword)
            return ApiResponse<int>.Fail("Passwords do not match.");

        if (!PasswordPolicyHelper.ValidatePassword(request.Password, out var policyError))
            return ApiResponse<int>.Fail(policyError);

        if (!UserValidators.ValidateCreateUser(request, out var validationError))
            return ApiResponse<int>.Fail(validationError);

        var (hash, salt) = _passwordHasher.Hash(request.Password);
        var newId = await InsertUserAsync(request, hash, salt, currentUserId);

        await LogAuditAsync(currentUserId, "Users", "Create", null, $"Created user '{request.Username}' (ID={newId})", null, null);
        _logger.LogInformation("User {Username} created by {Actor}.", request.Username, currentUserId);

        return ApiResponse<int>.Ok(newId, "User created successfully.");
    }

    public async Task<ApiResponse<object>> UpdateAsync(int id, UpdateUserRequest request, int currentUserId)
    {
        var existing = await GetDetailAsync(id);
        if (existing is null)
            return ApiResponse<object>.Fail("User not found.", 404);

        await _db.ExecuteNonQueryAsync("dbo.sp_UpdateUser", cmd =>
        {
            SqlDataAccess.AddParam(cmd, "@UserId", id);
            SqlDataAccess.AddParam(cmd, "@EmployeeNumber", request.EmployeeNumber);
            SqlDataAccess.AddParam(cmd, "@FullName", request.FullName);
            SqlDataAccess.AddParam(cmd, "@Email", request.Email);
            SqlDataAccess.AddParam(cmd, "@Phone", request.Phone);
            SqlDataAccess.AddParam(cmd, "@Designation", request.Designation);
            SqlDataAccess.AddParam(cmd, "@Department", request.Department);
            SqlDataAccess.AddParam(cmd, "@ProfilePicturePath", null);
            SqlDataAccess.AddParam(cmd, "@RoleId", request.RoleId);
            SqlDataAccess.AddParam(cmd, "@ModifiedBy", currentUserId);
        });

        await LogAuditAsync(currentUserId, "Users", "Update",
            $"FullName={existing.FullName}, Email={existing.Email}, RoleId={existing.RoleId}",
            $"FullName={request.FullName}, Email={request.Email}, RoleId={request.RoleId}", null, null);

        return ApiResponse<object>.Ok(null, "User updated successfully.");
    }

    public async Task<ApiResponse<object>> DeleteAsync(int id, int currentUserId)
    {
        if (id == currentUserId)
            return ApiResponse<object>.Fail("You cannot delete your own account.");

        var existing = await GetDetailAsync(id);
        if (existing is null)
            return ApiResponse<object>.Fail("User not found.", 404);

        await _db.ExecuteNonQueryAsync("dbo.sp_DeleteUser", cmd =>
        {
            SqlDataAccess.AddParam(cmd, "@UserId", id);
            SqlDataAccess.AddParam(cmd, "@ModifiedBy", currentUserId);
        });

        await LogAuditAsync(currentUserId, "Users", "Delete", $"UserId={id}", "Soft-deleted", null, null);

        return ApiResponse<object>.Ok(null, "User deleted successfully.");
    }

    public async Task<ApiResponse<object>> SetStatusAsync(int id, byte status, int currentUserId)
    {
        if (id == currentUserId && status == 0)
            return ApiResponse<object>.Fail("You cannot deactivate your own account.");

        await _db.ExecuteNonQueryAsync("dbo.sp_SetUserStatus", cmd =>
        {
            SqlDataAccess.AddParam(cmd, "@UserId", id);
            SqlDataAccess.AddParam(cmd, "@Status", status, SqlDbType.TinyInt);
            SqlDataAccess.AddParam(cmd, "@ModifiedBy", currentUserId);
        });

        await LogAuditAsync(currentUserId, "Users", "SetStatus", null, $"Status changed to {status} for UserId={id}", null, null);

        return ApiResponse<object>.Ok(null, status == 1 ? "User activated." : "User deactivated.");
    }

    public async Task<ApiResponse<UserPreferences?>> GetPreferencesAsync(int userId)
    {
        var prefs = await GetPreferencesInternalAsync(userId);
        return ApiResponse<UserPreferences?>.Ok(prefs ?? new UserPreferences { UserId = userId });
    }

    public async Task<ApiResponse<object>> SavePreferencesAsync(int userId, UserPreferences preferences)
    {
        preferences.UserId = userId;
        await _db.ExecuteNonQueryAsync("dbo.sp_SaveUserPreferences", cmd =>
        {
            SqlDataAccess.AddParam(cmd, "@UserId", userId);
            SqlDataAccess.AddParam(cmd, "@Theme", preferences.Theme);
            SqlDataAccess.AddParam(cmd, "@SidebarCollapsed", preferences.SidebarCollapsed);
            SqlDataAccess.AddParam(cmd, "@Language", preferences.Language);
            SqlDataAccess.AddParam(cmd, "@DashboardLayout", preferences.DashboardLayout);
            SqlDataAccess.AddParam(cmd, "@LandingPage", preferences.LandingPage);
        });
        return ApiResponse<object>.Ok(null, "Preferences saved successfully.");
    }

    // ------------------------------------------------------------------
    // Database access (stored procedures only)
    // ------------------------------------------------------------------

    private async Task<PagedResult<UserListItemDto>> SearchUsersAsync(UserSearchRequest request)
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
                reader.GetNullableString("EmployeeNumber"),
                reader.GetString(reader.GetOrdinal("Username")),
                reader.GetString(reader.GetOrdinal("FullName")),
                reader.GetString(reader.GetOrdinal("Email")),
                reader.GetNullableString("Phone"),
                reader.GetNullableString("Designation"),
                reader.GetNullableString("Department"),
                reader.GetNullableString("ProfilePicturePath"),
                reader.GetInt32(reader.GetOrdinal("RoleId")),
                reader.GetString(reader.GetOrdinal("RoleName")),
                reader.GetByte(reader.GetOrdinal("Status")),
                reader.GetNullableDateTime("LastLogin"),
                reader.GetInt32(reader.GetOrdinal("LoginCount")),
                reader.GetDateTime(reader.GetOrdinal("CreatedDate"))));
        }

        if (await reader.NextResultAsync() && await reader.ReadAsync())
        {
            result.TotalCount = reader.GetInt32(0);
        }

        return result;
    }

    private async Task<UserDetailDto?> GetDetailAsync(int userId) =>
        await _db.ExecuteReaderSingleAsync("dbo.sp_GetUserById",
            cmd => SqlDataAccess.AddParam(cmd, "@UserId", userId),
            reader => new UserDetailDto(
                reader.GetInt32(reader.GetOrdinal("UserId")),
                reader.GetNullableString("EmployeeNumber"),
                reader.GetString(reader.GetOrdinal("Username")),
                reader.GetString(reader.GetOrdinal("FullName")),
                reader.GetString(reader.GetOrdinal("Email")),
                reader.GetNullableString("Phone"),
                reader.GetNullableString("Designation"),
                reader.GetNullableString("Department"),
                reader.GetNullableString("ProfilePicturePath"),
                reader.GetInt32(reader.GetOrdinal("RoleId")),
                reader.GetString(reader.GetOrdinal("RoleName")),
                reader.GetByte(reader.GetOrdinal("Status")),
                reader.GetNullableDateTime("PasswordExpiryDate"),
                reader.GetBoolean(reader.GetOrdinal("MustChangePassword")),
                reader.GetBoolean(reader.GetOrdinal("IsLocked")),
                reader.GetNullableDateTime("LastLogin"),
                reader.GetInt32(reader.GetOrdinal("LoginCount")),
                reader.GetDateTime(reader.GetOrdinal("CreatedDate")),
                reader.GetNullableDateTime("ModifiedDate")));

    private async Task<int> InsertUserAsync(CreateUserRequest request, byte[] hash, byte[] salt, int createdBy)
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

    private async Task<UserPreferences?> GetPreferencesInternalAsync(int userId) =>
        await _db.ExecuteReaderSingleAsync("dbo.sp_GetUserPreferences",
            cmd => SqlDataAccess.AddParam(cmd, "@UserId", userId),
            reader => new UserPreferences
            {
                UserId = reader.GetInt32(reader.GetOrdinal("UserId")),
                Theme = reader.GetString(reader.GetOrdinal("Theme")),
                SidebarCollapsed = reader.GetBoolean(reader.GetOrdinal("SidebarCollapsed")),
                Language = reader.GetString(reader.GetOrdinal("Language")),
                DashboardLayout = reader.GetNullableString("DashboardLayout"),
                LandingPage = reader.GetNullableString("LandingPage")
            });

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
