using System.Security.Claims;
using MyApp.Api.Services.Interfaces;

namespace MyApp.Api.Services.Implementations;

/// <summary>
/// Reads the authenticated user's identity out of the session-token claims
/// populated by <c>SessionTokenAuthenticationHandler</c>.
/// </summary>
public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _accessor;

    public CurrentUserService(IHttpContextAccessor accessor)
    {
        _accessor = accessor;
    }

    private ClaimsPrincipal? User => _accessor.HttpContext?.User;

    public int? UserId => ParseInt(User?.FindFirstValue(ClaimTypes.NameIdentifier));
    public string? Username => User?.FindFirstValue(ClaimTypes.Name);
    public string? RoleName => User?.FindFirstValue(ClaimTypes.Role);
    public int? RoleId => ParseInt(User?.FindFirstValue("RoleId"));

    public string? IpAddress => _accessor.HttpContext?.Connection.RemoteIpAddress?.ToString();
    public string? UserAgent => _accessor.HttpContext?.Request.Headers.UserAgent.ToString();

    private static int? ParseInt(string? value) => int.TryParse(value, out var id) ? id : null;
}
