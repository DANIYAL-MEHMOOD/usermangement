using MyApp.Api.Common;
using MyApp.Api.Data;
using MyApp.Api.DTOs;
using MyApp.Api.Services.Interfaces;

namespace MyApp.Api.Services.Implementations;

/// <summary>
/// Executive dashboard: KPI statistics, recent logins, activity feed and
/// role statistics — one stored procedure, four result sets.
/// </summary>
public class DashboardService : IDashboardService
{
    private readonly SqlDataAccess _db;

    public DashboardService(SqlDataAccess db)
    {
        _db = db;
    }

    public async Task<ApiResponse<DashboardSummary>> GetSummaryAsync()
    {
        var summary = new DashboardSummary();

        await using var connection = await _db.OpenConnectionAsync();
        await using var command = SqlDataAccess.CreateCommand(connection, "dbo.sp_GetDashboard");
        SqlDataAccess.AddParam(command, "@RecentCount", 10);

        await using var reader = await command.ExecuteReaderAsync();

        // 1) Summary counts: TotalUsers, ActiveUsers, LockedUsers, TotalRoles, TotalMenus
        if (await reader.ReadAsync())
        {
            summary.TotalUsers = reader.GetInt32(0);
            summary.ActiveUsers = reader.GetInt32(1);
            summary.LockedUsers = reader.GetInt32(2);
            summary.TotalRoles = reader.GetInt32(3);
            summary.TotalMenus = reader.GetInt32(4);
        }

        // 2) Recent logins
        await reader.NextResultAsync();
        while (await reader.ReadAsync())
        {
            var fullName = reader.GetString(reader.GetOrdinal("FullName"));
            var lastLogin = reader.GetNullableDateTime("LastLogin");
            summary.RecentLogins.Add(new RecentActivityDto(
                "Auth", "LoginSuccess", "Signed in", lastLogin ?? DateTime.UtcNow, fullName));
        }

        // 3) Recent activity (audit feed)
        await reader.NextResultAsync();
        while (await reader.ReadAsync())
        {
            summary.AuditLogs.Add(new RecentActivityDto(
                reader.GetString(reader.GetOrdinal("Module")),
                reader.GetString(reader.GetOrdinal("Action")),
                reader.GetNullableString("FullName") ?? "System",
                reader.GetDateTime(reader.GetOrdinal("ActionDate")),
                reader.GetNullableString("FullName")));
        }

        // 4) Role statistics (exposed via TotalRoles plus role breakdown for future charts)
        await reader.NextResultAsync();

        return ApiResponse<DashboardSummary>.Ok(summary);
    }
}
