using BookRatingAPI.DTOs;

namespace BookRatingAPI.Services;

public interface IProfileService
{
    Task<ProfileDto?> GetMyProfileAsync(int userId);
    Task<PublicProfileDto?> GetUserProfileAsync(int userId);
}
