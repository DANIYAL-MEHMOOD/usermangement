using MyApp.Api.Models;

namespace MyApp.Api.Repository.Interfaces;

public interface IAuthRepository
{
    Task<User?> GetForLoginAsync(string username);
    Task RecordLoginSuccessAsync(int userId);
    Task RecordLoginFailureAsync(int userId, int maxAttempts = 5, int lockoutMinutes = 15);
    Task UnlockExpiredLockoutsAsync();
    Task ChangePasswordAsync(int userId, byte[] newHash, byte[] newSalt, int expiryDays = 90);
    Task<List<(byte[] Hash, byte[] Salt)>> GetPasswordHistoryAsync(int userId, int historyCount = 5);
    Task<int> CreatePasswordResetTokenAsync(int userId, string token, int expiryMinutes = 30);
    Task<(int TokenId, int UserId, DateTime ExpiryDate, bool IsUsed)?> ValidatePasswordResetTokenAsync(string token);
    Task ConsumePasswordResetTokenAsync(int tokenId);
    Task SaveRefreshTokenAsync(int userId, string token, DateTime expiryDate, string? createdByIp);
    Task<RefreshToken?> GetRefreshTokenAsync(string token);
    Task RevokeRefreshTokenAsync(string token, string? revokedByIp, string? replacedByToken = null);
}
