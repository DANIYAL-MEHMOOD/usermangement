using MyApp.Api.Common;
using MyApp.Api.DTOs;
using MyApp.Api.Repository.Interfaces;
using MyApp.Api.Services.Interfaces;

namespace MyApp.Api.Services.Implementations;

public class DashboardService : IDashboardService
{
    private readonly IDashboardRepository _dashboard;

    public DashboardService(IDashboardRepository dashboard)
    {
        _dashboard = dashboard;
    }

    public async Task<ApiResponse<DashboardSummary>> GetSummaryAsync()
    {
        var summary = await _dashboard.GetSummaryAsync();
        return ApiResponse<DashboardSummary>.Ok(summary);
    }
}
