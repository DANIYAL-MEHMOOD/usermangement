namespace MyApp.Api.Security;

public class JwtSettings
{
    public const string SectionName = "Jwt";
    public required string Issuer { get; set; }
    public required string Audience { get; set; }
    public required string SigningKey { get; set; }
    public int AccessTokenMinutes { get; set; } = 15;
    public int RefreshTokenDays { get; set; } = 7;
    public int RefreshTokenDaysRememberMe { get; set; } = 30;
}
