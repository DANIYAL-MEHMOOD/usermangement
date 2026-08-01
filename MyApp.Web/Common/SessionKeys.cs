namespace MyApp.Web.Common;

public static class SessionKeys
{
    public const string AccessToken = "AccessToken";
    public const string RefreshToken = "RefreshToken";
    public const string AccessTokenExpiry = "AccessTokenExpiry"; // stored as round-trip ("o") UTC string
    public const string UserId = "UserId";
    public const string Username = "Username";
    public const string FullName = "FullName";
    public const string RoleName = "RoleName";
    public const string Theme = "Theme";
    public const string Language = "Language";
    public const string SidebarCollapsed = "SidebarCollapsed";
}
