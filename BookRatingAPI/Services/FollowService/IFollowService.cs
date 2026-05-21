using BookRatingAPI.DTOs.FollowDTOs;

namespace BookRatingAPI.Services;

public interface IFollowService
{
    Task<(bool Success, string? Error)> FollowAsync(int followerId, int followeeId);
    Task<(bool Success, string? Error)> UnfollowAsync(int followerId, int followeeId);

    Task<List<FollowUserDto>?> GetFollowersAsync(int userId);
    Task<List<FollowUserDto>?> GetFollowingAsync(int userId);
    Task<FollowStatsDto?> GetFollowStatsAsync(int userId);

    Task<ActivityFeedDto> GetActivityFeedAsync(int userId, int page, int pageSize);
}
