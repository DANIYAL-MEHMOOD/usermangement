using MyApp.Web.Common;
using MyApp.Web.DTOs;

namespace MyApp.Web.Services.Interfaces;

public interface IProfileService
{
    Task<ApiResponse<ProfileDto?>?> GetProfileAsync();
    Task<ApiResponse<object>?> UpdateProfileAsync(UpdateProfileDto request);
    Task<ApiResponse<UpdatePreferencesDto?>?> GetPreferencesAsync();
    Task<ApiResponse<object>?> UpdatePreferencesAsync(UpdatePreferencesDto request);
}

public interface IDashboardService
{
    Task<ApiResponse<DashboardSummaryDto>?> GetSummaryAsync();
}

public interface IAuditService
{
    Task<ApiResponse<List<AuditLogItemDto>>?> SearchAsync(AuditLogSearchRequestDto request);
}
