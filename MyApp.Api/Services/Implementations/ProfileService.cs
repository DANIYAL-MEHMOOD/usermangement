using MyApp.Api.Common;
using MyApp.Api.DTOs;
using MyApp.Api.Models;
using MyApp.Api.Repository.Interfaces;
using MyApp.Api.Services.Interfaces;

namespace MyApp.Api.Services.Implementations;

public class ProfileService : IProfileService
{
    private readonly IProfileRepository _profileRepo;
    private readonly IUserRepository _userRepo;
    private readonly IAuditLogRepository _auditLog;

    public ProfileService(IProfileRepository profileRepo, IUserRepository userRepo, IAuditLogRepository auditLog)
    {
        _profileRepo = profileRepo;
        _userRepo = userRepo;
        _auditLog = auditLog;
    }

    public async Task<ApiResponse<ProfileDto?>> GetProfileAsync(int userId)
    {
        var profile = await _profileRepo.GetProfileAsync(userId);
        if (profile is null)
            return ApiResponse<ProfileDto?>.Fail("Profile not found.");
        return ApiResponse<ProfileDto?>.Ok(profile);
    }

    public async Task<ApiResponse<object>> UpdateProfileAsync(int userId, UpdateProfileDto request, int currentUserId)
    {
        var profile = await _profileRepo.GetProfileAsync(userId);
        if (profile is null)
            return ApiResponse<object>.Fail("Profile not found.");

        await _profileRepo.UpdateProfileAsync(userId, request, currentUserId);
        await _auditLog.InsertAsync(currentUserId, "Profile", "Update",
            $"FullName={profile.FullName}, Email={profile.Email}",
            $"FullName={request.FullName}, Email={request.Email}", null, null);

        return ApiResponse<object>.Ok(null, "Profile updated successfully.");
    }

    public async Task<ApiResponse<UserPreferences?>> GetPreferencesAsync(int userId)
    {
        var prefs = await _userRepo.GetPreferencesAsync(userId);
        return ApiResponse<UserPreferences?>.Ok(prefs ?? new UserPreferences { UserId = userId });
    }

    public async Task<ApiResponse<object>> UpdatePreferencesAsync(int userId, UpdatePreferencesDto request)
    {
        var prefs = new UserPreferences
        {
            UserId = userId,
            Theme = request.Theme,
            SidebarCollapsed = request.SidebarCollapsed,
            Language = request.Language,
            DashboardLayout = request.DashboardLayout,
            LandingPage = request.LandingPage
        };

        await _userRepo.SavePreferencesAsync(userId, prefs);
        await _auditLog.InsertAsync(userId, "Settings", "UpdatePreferences", null,
            $"Theme={request.Theme}, Language={request.Language}", null, null);

        return ApiResponse<object>.Ok(null, "Settings updated successfully.");
    }
}
