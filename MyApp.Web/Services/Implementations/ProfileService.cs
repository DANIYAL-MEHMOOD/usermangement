using MyApp.Web.ApiClient;
using MyApp.Web.Common;
using MyApp.Web.DTOs;
using MyApp.Web.Services.Interfaces;

namespace MyApp.Web.Services.Implementations;

public class ProfileService : IProfileService
{
    private readonly IApiClient _api;

    public ProfileService(IApiClient api)
    {
        _api = api;
    }

    public Task<ApiResponse<ProfileDto?>?> GetProfileAsync() =>
        _api.GetAsync<ProfileDto?>("api/profile");

    public Task<ApiResponse<object>?> UpdateProfileAsync(UpdateProfileDto request) =>
        _api.PutAsync<UpdateProfileDto, object>("api/profile", request);

    public Task<ApiResponse<UpdatePreferencesDto?>?> GetPreferencesAsync() =>
        _api.GetAsync<UpdatePreferencesDto?>("api/profile/preferences");

    public Task<ApiResponse<object>?> UpdatePreferencesAsync(UpdatePreferencesDto request) =>
        _api.PutAsync<UpdatePreferencesDto, object>("api/profile/preferences", request);
}

public class DashboardService : IDashboardService
{
    private readonly IApiClient _api;

    public DashboardService(IApiClient api)
    {
        _api = api;
    }

    public Task<ApiResponse<DashboardSummaryDto>?> GetSummaryAsync() =>
        _api.GetAsync<DashboardSummaryDto>("api/dashboard");
}

public class AuditService : IAuditService
{
    private readonly IApiClient _api;

    public AuditService(IApiClient api)
    {
        _api = api;
    }

    public Task<ApiResponse<List<AuditLogItemDto>>?> SearchAsync(AuditLogSearchRequestDto request)
    {
        var query = $"api/auditlogs?SearchTerm={Uri.EscapeDataString(request.SearchTerm ?? "")}&Module={Uri.EscapeDataString(request.Module ?? "")}&Action={Uri.EscapeDataString(request.Action ?? "")}&PageNumber={request.PageNumber}&PageSize={request.PageSize}";
        return _api.GetAsync<List<AuditLogItemDto>>(query);
    }
}
