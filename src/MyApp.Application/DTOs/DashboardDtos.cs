namespace MyApp.Application.DTOs;

public record DashboardSummaryDto(int TotalUsers, int ActiveUsers, int OnlineUsers, int TotalRoles);
public record RecentLoginDto(int UserId, string Username, string FullName, DateTime? LastLogin);
public record RecentActivityDto(long AuditLogId, int? UserId, string? FullName, string Module, string Action, DateTime ActionDate);
public record RoleStatDto(int RoleId, string RoleName, int UserCount);

public class DashboardDto
{
    public DashboardSummaryDto Summary { get; set; } = null!;
    public List<RecentLoginDto> RecentLogins { get; set; } = [];
    public List<RecentActivityDto> RecentActivity { get; set; } = [];
    public List<RoleStatDto> RoleStats { get; set; } = [];
}
