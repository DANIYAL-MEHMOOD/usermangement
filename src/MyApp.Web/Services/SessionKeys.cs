namespace MyApp.Web.Services;

/// <summary>Centralizes the session keys the Web project uses to hold the
/// authenticated user's tokens and identity — avoids typo drift across pages.</summary>
public static class SessionKeys
{
    public const string AccessToken = "AccessToken";
    public const string RefreshToken = "RefreshToken";
    public const string AccessTokenExpiry = "AccessTokenExpiry"; // stored as round-trip ("o") UTC string
    public const string UserId = "UserId";
    public const string Username = "Username";
    public const string FullName = "FullName";
    public const string RoleName = "RoleName";
}
