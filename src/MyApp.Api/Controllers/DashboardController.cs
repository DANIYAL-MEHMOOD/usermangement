using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyApp.Application.Common;
using MyApp.Application.DTOs;
using MyApp.Application.Interfaces;

namespace MyApp.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class DashboardController : ControllerBase
{
    private readonly IDashboardRepository _dashboard;
    public DashboardController(IDashboardRepository dashboard) => _dashboard = dashboard;

    [HttpGet]
    public async Task<ActionResult<ApiResponse<DashboardDto>>> Get([FromQuery] int recentCount = 10)
    {
        var (summary, recentLogins, recentActivity, roleStats) = await _dashboard.GetDashboardAsync(recentCount);

        var dto = new DashboardDto
        {
            Summary = new DashboardSummaryDto(summary.TotalUsers, summary.ActiveUsers, summary.OnlineUsers, summary.TotalRoles),
            RecentLogins = recentLogins.Select(l => new RecentLoginDto(l.UserId, l.Username, l.FullName, l.LastLogin)).ToList(),
            RecentActivity = recentActivity.Select(a => new RecentActivityDto(a.AuditLogId, a.UserId, a.FullName, a.Module, a.Action, a.ActionDate)).ToList(),
            RoleStats = roleStats.Select(r => new RoleStatDto(r.RoleId, r.RoleName, r.UserCount)).ToList()
        };

        return Ok(ApiResponse<DashboardDto>.Ok(dto));
    }
}
