using System.Threading.Tasks;
using BookRatingAPI.DTOs;
using BookRatingAPI.DTOs.ProfileDTOs;

namespace BookRatingAPI.Services;

public interface IProfileService
{
    Task<ProfileDto?> GetMyProfileAsync(int userId);
    Task<PublicProfileDto?> GetUserProfileAsync(int userId);
    Task<ReadingStatsDto?> GetReadingStatsAsync(int userId);
}
