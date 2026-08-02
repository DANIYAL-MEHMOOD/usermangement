namespace MyApp.Api.DTOs;

/// <summary>
/// Audit log row. The SQL layer returns FullName / ActionDate; this DTO maps
/// them to Username / CreatedDate so the Web client model stays stable.
/// </summary>
public record AuditLogItem(
    long AuditLogId, int? UserId, string? Username, string Module,
    string Action, string? OldValue, string? NewValue, string? Browser,
    string? IPAddress, DateTime CreatedDate);

public class AuditLogSearchRequest
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
