namespace MyApp.Api.Configurations;

public class DatabaseSettings
{
    public string ConnectionString { get; set; } = string.Empty;
}

/// <summary>Security policy knobs (lockout, password expiry, session token lifetimes).</summary>
public class SecuritySettings
{
    public int PasswordExpiryDays { get; set; } = 90;
    public int MaxFailedLoginAttempts { get; set; } = 5;
    public int AccountLockoutMinutes { get; set; } = 15;
    /// <summary>Session token lifetime (hours) for a normal login.</summary>
    public int SessionTokenHours { get; set; } = 8;
    /// <summary>Session token lifetime (days) for a "remember me" login.</summary>
    public int SessionTokenRememberDays { get; set; } = 30;
    /// <summary>
    /// Development convenience only: when true the password-reset link is
    /// returned in the API response so the flow can be exercised without a
    /// mail server. In production keep this false and email the link instead.
    /// </summary>
    public bool ExposeResetLinkInResponse { get; set; }
}
