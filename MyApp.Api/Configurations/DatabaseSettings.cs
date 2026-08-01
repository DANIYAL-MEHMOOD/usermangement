namespace MyApp.Api.Configurations;

public class DatabaseSettings
{
    public string ConnectionString { get; set; } = string.Empty;
}

public class SecuritySettings
{
    public int PasswordExpiryDays { get; set; } = 90;
    public int MaxFailedLoginAttempts { get; set; } = 5;
    public int AccountLockoutMinutes { get; set; } = 15;
}
