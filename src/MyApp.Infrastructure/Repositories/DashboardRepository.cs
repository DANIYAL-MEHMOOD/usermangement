using MyApp.Application.Interfaces;
using MyApp.Infrastructure.Data;

namespace MyApp.Infrastructure.Repositories;

public class DashboardRepository : IDashboardRepository
{
    private readonly SqlDataAccess _db;
    public DashboardRepository(SqlDataAccess db) => _db = db;

    public async Task<(
        (int TotalUsers, int ActiveUsers, int OnlineUsers, int TotalRoles) Summary,
        List<(int UserId, string Username, string FullName, DateTime? LastLogin)> RecentLogins,
        List<(long AuditLogId, int? UserId, string? FullName, string Module, string Action, DateTime ActionDate)> RecentActivity,
        List<(int RoleId, string RoleName, int UserCount)> RoleStats
    )> GetDashboardAsync(int recentCount = 10)
    {
        await using var connection = await _db.OpenConnectionAsync();
        await using var command = SqlDataAccess.CreateCommand(connection, "dbo.sp_GetDashboard");
        SqlDataAccess.AddParam(command, "@RecentCount", recentCount);

        await using var reader = await command.ExecuteReaderAsync();

        await reader.ReadAsync();
        var summary = (reader.GetInt32(0), reader.GetInt32(1), reader.GetInt32(2), reader.GetInt32(3));

        var recentLogins = new List<(int, string, string, DateTime?)>();
        await reader.NextResultAsync();
        while (await reader.ReadAsync())
        {
            recentLogins.Add((
                reader.GetInt32(reader.GetOrdinal("UserId")),
                reader.GetString(reader.GetOrdinal("Username")),
                reader.GetString(reader.GetOrdinal("FullName")),
                reader.IsDBNull(reader.GetOrdinal("LastLogin")) ? null : reader.GetDateTime(reader.GetOrdinal("LastLogin"))));
        }

        var recentActivity = new List<(long, int?, string?, string, string, DateTime)>();
        await reader.NextResultAsync();
        while (await reader.ReadAsync())
        {
            recentActivity.Add((
                reader.GetInt64(reader.GetOrdinal("AuditLogId")),
                reader.IsDBNull(reader.GetOrdinal("UserId")) ? null : reader.GetInt32(reader.GetOrdinal("UserId")),
                reader.IsDBNull(reader.GetOrdinal("FullName")) ? null : reader.GetString(reader.GetOrdinal("FullName")),
                reader.GetString(reader.GetOrdinal("Module")),
                reader.GetString(reader.GetOrdinal("Action")),
                reader.GetDateTime(reader.GetOrdinal("ActionDate"))));
        }

        var roleStats = new List<(int, string, int)>();
        await reader.NextResultAsync();
        while (await reader.ReadAsync())
        {
            roleStats.Add((
                reader.GetInt32(reader.GetOrdinal("RoleId")),
                reader.GetString(reader.GetOrdinal("RoleName")),
                reader.GetInt32(reader.GetOrdinal("UserCount"))));
        }

        return (summary, recentLogins, recentActivity, roleStats);
    }
}
