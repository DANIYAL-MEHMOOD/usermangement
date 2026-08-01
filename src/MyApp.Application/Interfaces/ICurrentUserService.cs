namespace MyApp.Application.Interfaces;

/// <summary>Reads the authenticated user's identity out of the current HTTP context (implemented in the API layer).</summary>
public interface ICurrentUserService
{
    int? UserId { get; }
    string? Username { get; }
    string? RoleName { get; }
    int? RoleId { get; }
    string? IpAddress { get; }
    string? UserAgent { get; }
}
