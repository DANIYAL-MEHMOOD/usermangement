using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace MyApp.Api.Security;

/// <summary>
/// Custom authentication handler for the opaque session-token scheme.
/// The Web layer forwards the token on the <c>X-Api-Token</c> header; the
/// handler validates it against the database (SHA-256 hash lookup) and builds
/// the ClaimsPrincipal. No JWT is used anywhere in the solution.
/// </summary>
public class SessionTokenAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    private readonly ISessionTokenService _sessions;

    public SessionTokenAuthenticationHandler(
        ISessionTokenService sessions,
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder)
        : base(options, logger, encoder)
    {
        _sessions = sessions;
    }

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue(SessionTokenDefaults.HeaderName, out var headerValues))
        {
            return AuthenticateResult.NoResult();
        }

        var token = headerValues.FirstOrDefault();
        if (string.IsNullOrWhiteSpace(token))
        {
            return AuthenticateResult.NoResult();
        }

        SessionTokenPrincipal? session;
        try
        {
            session = await _sessions.ValidateAsync(token, Context.RequestAborted);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Session token validation failed.");
            return AuthenticateResult.Fail("Session token validation failed.");
        }

        if (session is null)
        {
            return AuthenticateResult.Fail("Invalid or expired session token.");
        }

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, session.UserId.ToString()),
            new Claim(ClaimTypes.Name, session.Username),
            new Claim(ClaimTypes.Role, session.RoleName),
            new Claim("RoleId", session.RoleId.ToString()),
            new Claim("SessionTokenId", session.SessionTokenId.ToString())
        };

        var identity = new ClaimsIdentity(claims, Scheme.Name);
        var principal = new ClaimsPrincipal(identity);
        return AuthenticateResult.Success(new AuthenticationTicket(principal, Scheme.Name));
    }
}
