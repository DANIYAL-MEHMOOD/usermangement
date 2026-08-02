namespace MyApp.Api.Security;

public static class SessionTokenDefaults
{
    public const string AuthenticationScheme = "SessionToken";
    /// <summary>Header that carries the opaque session token from the Web layer.</summary>
    public const string HeaderName = "X-Api-Token";
}
