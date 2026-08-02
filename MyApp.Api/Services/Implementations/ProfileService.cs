using MyApp.Api.Common;
using MyApp.Api.Data;
using MyApp.Api.DTOs;
using MyApp.Api.Models;
using MyApp.Api.Services.Interfaces;

namespace MyApp.Api.Services.Implementations;

/// <summary>
/// User profile and preferences (theme, sidebar, language, layout, landing
/// page). Database access via stored procedures and SqlDataAccess.
/// </summary>
public class ProfileService : IProfileService
{
    private readonly SqlDataAccess _db;

    public ProfileService(SqlDataAccess db)
    {
        _db = db;
    }

    public async Task<ApiResponse<ProfileDto?>> GetProfileAsync(int userId)
    {
        var profile = await _db.ExecuteReaderSingleAsync("dbo.sp_GetProfile",
            cmd => SqlDataAccess.AddParam(cmd, "@UserId", userId),
            MapProfile);

        if (profile is null)
            return ApiResponse<ProfileDto?>.Fail("Profile not found.", 404);

        return ApiResponse<ProfileDto?>.Ok(profile);
    }

    public async Task<ApiResponse<object>> UpdateProfileAsync(int userId, UpdateProfileDto request, int currentUserId)
    {
        var profile = await _db.ExecuteReaderSingleAsync("dbo.sp_GetProfile",
            cmd => SqlDataAccess.AddParam(cmd, "@UserId", userId),
            MapProfile);

        if (profile is null)
            return ApiResponse<object>.Fail("Profile not found.", 404);

        await _db.ExecuteNonQueryAsync("dbo.sp_UpdateProfile", cmd =>
        {
            SqlDataAccess.AddParam(cmd, "@UserId", userId);
            SqlDataAccess.AddParam(cmd, "@FullName", request.FullName);
            SqlDataAccess.AddParam(cmd, "@Email", request.Email);
            SqlDataAccess.AddParam(cmd, "@Phone", request.Phone);
            SqlDataAccess.AddParam(cmd, "@Designation", request.Designation);
            SqlDataAccess.AddParam(cmd, "@Department", request.Department);
            SqlDataAccess.AddParam(cmd, "@ProfilePicturePath", request.ProfilePicturePath);
            SqlDataAccess.AddParam(cmd, "@ModifiedBy", currentUserId);
        });

        await LogAuditAsync(currentUserId, "Profile", "Update",
            $"FullName={profile.FullName}, Email={profile.Email}",
            $"FullName={request.FullName}, Email={request.Email}", null, null);

        return ApiResponse<object>.Ok(null, "Profile updated successfully.");
    }

    public async Task<ApiResponse<UserPreferences?>> GetPreferencesAsync(int userId)
    {
        var prefs = await GetPreferencesInternalAsync(userId);
        return ApiResponse<UserPreferences?>.Ok(prefs ?? new UserPreferences { UserId = userId });
    }

    public async Task<ApiResponse<object>> UpdatePreferencesAsync(int userId, UpdatePreferencesDto request)
    {
        await _db.ExecuteNonQueryAsync("dbo.sp_SaveUserPreferences", cmd =>
        {
            SqlDataAccess.AddParam(cmd, "@UserId", userId);
            SqlDataAccess.AddParam(cmd, "@Theme", request.Theme);
            SqlDataAccess.AddParam(cmd, "@SidebarCollapsed", request.SidebarCollapsed);
            SqlDataAccess.AddParam(cmd, "@Language", request.Language);
            SqlDataAccess.AddParam(cmd, "@DashboardLayout", request.DashboardLayout);
            SqlDataAccess.AddParam(cmd, "@LandingPage", request.LandingPage);
        });

        await LogAuditAsync(userId, "Settings", "UpdatePreferences", null,
            $"Theme={request.Theme}, Language={request.Language}", null, null);

        return ApiResponse<object>.Ok(null, "Settings updated successfully.");
    }

    // ------------------------------------------------------------------
    // Database access (stored procedures only)
    // ------------------------------------------------------------------

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

    private static ProfileDto MapProfile(SqlDataReader reader) => new()
    {
        UserId = reader.GetInt32(reader.GetOrdinal("UserId")),
        EmployeeNumber = reader.GetNullableString("EmployeeNumber"),
        Username = reader.GetString(reader.GetOrdinal("Username")),
        FullName = reader.GetString(reader.GetOrdinal("FullName")),
        Email = reader.GetString(reader.GetOrdinal("Email")),
        Phone = reader.GetNullableString("Phone"),
        Designation = reader.GetNullableString("Designation"),
        Department = reader.GetNullableString("Department"),
        ProfilePicturePath = reader.GetNullableString("ProfilePicturePath"),
        RoleId = reader.GetInt32(reader.GetOrdinal("RoleId")),
        RoleName = reader.GetString(reader.GetOrdinal("RoleName")),
        LastLogin = reader.GetNullableDateTime("LastLogin"),
        Theme = reader.GetNullableString("Theme") ?? "light",
        SidebarCollapsed = reader.GetBoolean(reader.GetOrdinal("SidebarCollapsed")),
        Language = reader.GetNullableString("Language") ?? "en",
        DashboardLayout = reader.GetNullableString("DashboardLayout"),
        LandingPage = reader.GetNullableString("LandingPage")
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
