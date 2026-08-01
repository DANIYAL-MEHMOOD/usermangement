using MyApp.Api.Common;
using MyApp.Api.DTOs;

namespace MyApp.Api.Services.Interfaces;

public interface IDashboardService
{
    Task<ApiResponse<DashboardSummary>> GetSummaryAsync();
}
