using MyApp.Api.DTOs;

namespace MyApp.Api.Repository.Interfaces;

public interface IProfileRepository
{
    Task<ProfileDto?> GetProfileAsync(int userId);
    Task UpdateProfileAsync(int userId, UpdateProfileDto request, int modifiedBy);
}
