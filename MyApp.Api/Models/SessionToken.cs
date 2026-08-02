namespace MyApp.Api.Models;

/// <summary>A single active API session token (Session Management module).</summary>
public class SessionToken
{
    public int SessionTokenId { get; set; }
    public int UserId { get; set; }
    public string TokenHash { get; set; } = string.Empty;
    public DateTime ExpiryDate { get; set; }
    public DateTime CreatedDate { get; set; }
    public string? CreatedByIp { get; set; }
    public string? UserAgent { get; set; }
    public bool IsRevoked { get; set; }
    public DateTime? RevokedDate { get; set; }

    /// <summary>Computed per-request; true when this session is the one presenting the current token.</summary>
    public bool IsCurrent { get; set; }
}
