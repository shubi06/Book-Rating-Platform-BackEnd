using BookRatingAPI.DTOs.FollowDTOs;

namespace BookRatingAPI.Services;

public interface IFollowService
{
    Task<FollowOperationResult> FollowAsync(int followerId, int followeeId);
    Task<FollowOperationResult> UnfollowAsync(int followerId, int followeeId);

    Task<List<FollowUserDto>?> GetFollowersAsync(int userId);
    Task<List<FollowUserDto>?> GetFollowingAsync(int userId);
    Task<FollowStatsDto?> GetFollowStatsAsync(int userId);

    Task<ActivityFeedDto> GetActivityFeedAsync(int userId, int page, int pageSize);
}
