namespace MyApp.Web.Models;

public record StatCard(string Title, int Value, string Icon, string Color, string? Subtitle = null);

public record RecentActivity(string Module, string Action, string Details, DateTime Timestamp, string? User = null);

public class DashboardSummary
{
    public int TotalUsers { get; set; }
    public int ActiveUsers { get; set; }
    public int LockedUsers { get; set; }
    public int TotalRoles { get; set; }
    public int TotalMenus { get; set; }
    public List<RecentActivity> RecentLogins { get; set; } = [];
    public List<RecentActivity> AuditLogs { get; set; } = [];
}
