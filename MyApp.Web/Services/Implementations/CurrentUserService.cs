using MyApp.Web.Common;
using MyApp.Web.Services.Interfaces;

namespace MyApp.Web.Services.Implementations;

public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _accessor;

    public CurrentUserService(IHttpContextAccessor accessor)
    {
        _accessor = accessor;
    }

    private ISession? Session => _accessor.HttpContext?.Session;

    public int UserId => Session?.GetInt32(SessionKeys.UserId) ?? 0;
    public string Username => Session?.GetString(SessionKeys.Username) ?? string.Empty;
    public string FullName => Session?.GetString(SessionKeys.FullName) ?? string.Empty;
    public string RoleName => Session?.GetString(SessionKeys.RoleName) ?? string.Empty;
    public string ApiToken => Session?.GetString(SessionKeys.ApiToken) ?? string.Empty;
    public bool IsAuthenticated => !string.IsNullOrEmpty(ApiToken);
    public bool IsAdministrator => string.Equals(RoleName, "Administrator", StringComparison.OrdinalIgnoreCase);
}
