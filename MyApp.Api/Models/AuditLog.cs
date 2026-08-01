namespace MyApp.Api.Models;

public class AuditLog
{
    public long AuditLogId { get; set; }
    public int? UserId { get; set; }
    public string? FullName { get; set; }
    public string Module { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string? OldValue { get; set; }
    public string? NewValue { get; set; }
    public string? Browser { get; set; }
    public string? IPAddress { get; set; }
    public DateTime ActionDate { get; set; }
}
