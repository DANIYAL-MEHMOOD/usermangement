namespace MyApp.Application.Interfaces;

public interface IJwtTokenGenerator
{
    (string Token, DateTime Expiry) GenerateAccessToken(int userId, string username, string roleName);
    string GenerateRefreshToken();
}
