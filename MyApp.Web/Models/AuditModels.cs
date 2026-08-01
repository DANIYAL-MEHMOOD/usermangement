namespace MyApp.Web.Models;

public record AuditLogItem(
    long AuditLogId, int? UserId, string? Username, string Module,
    string Action, string? OldValue, string? NewValue, string? Browser,
    string? IPAddress, DateTime CreatedDate);

public class AuditLogSearchModel
{
    public string? SearchTerm { get; set; }
    public string? Module { get; set; }
    public string? Action { get; set; }
    public int? UserId { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 25;
}
