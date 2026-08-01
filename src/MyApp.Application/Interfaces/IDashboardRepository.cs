namespace MyApp.Application.Interfaces;

public interface IDashboardRepository
{
    Task<(
        (int TotalUsers, int ActiveUsers, int OnlineUsers, int TotalRoles) Summary,
        List<(int UserId, string Username, string FullName, DateTime? LastLogin)> RecentLogins,
        List<(long AuditLogId, int? UserId, string? FullName, string Module, string Action, DateTime ActionDate)> RecentActivity,
        List<(int RoleId, string RoleName, int UserCount)> RoleStats
    )> GetDashboardAsync(int recentCount = 10);
}
