namespace MyApp.Api.DTOs;

/// <summary>Executive dashboard payload consumed by the MVC dashboard view.</summary>
public class DashboardSummary
{
    public int TotalUsers { get; set; }
    public int ActiveUsers { get; set; }
    public int LockedUsers { get; set; }
    public int TotalRoles { get; set; }
    public int TotalMenus { get; set; }
    public List<RecentActivityDto> RecentLogins { get; set; } = [];
    public List<RecentActivityDto> AuditLogs { get; set; } = [];
}

public record RecentActivityDto(string Module, string Action, string Details, DateTime Timestamp, string? User = null);
