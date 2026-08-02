namespace MyApp.Web.Common;

public static class SessionKeys
{
    /// <summary>Opaque API session token returned by the API at login. Stored only in
    /// the server-side session — never sent to browser JavaScript.</summary>
    public const string ApiToken = "ApiToken";
    public const string ApiTokenExpiry = "ApiTokenExpiry"; // round-trip ("o") UTC string
    public const string UserId = "UserId";
    public const string Username = "Username";
    public const string FullName = "FullName";
    public const string RoleName = "RoleName";
    public const string Theme = "Theme";
    public const string Language = "Language";
    public const string SidebarCollapsed = "SidebarCollapsed";
}
