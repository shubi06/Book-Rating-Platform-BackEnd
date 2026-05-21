using System.Security.Claims;
using System.Threading.Tasks;
using BookRatingAPI.DTOs;
using BookRatingAPI.DTOs.FollowDTOs;
using BookRatingAPI.DTOs.ProfileDTOs;
using BookRatingAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace BookRatingAPI.Controllers;

/// <summary>
/// Controller for managing user profiles
/// </summary>
[Authorize]
[ApiController]
[Route("api/[controller]")]
public class ProfileController : ControllerBase
{
    private readonly IProfileService _profileService;
    private readonly IFollowService _followService;
    private readonly ILogger<ProfileController> _logger;

    public ProfileController(
        IProfileService profileService,
        IFollowService followService,
        ILogger<ProfileController> logger)
    {
        _profileService = profileService;
        _followService = followService;
        _logger = logger;
    }

    /// <summary>
    /// Get the authenticated user's profile
    /// </summary>
    /// <returns>User profile with statistics</returns>
    [HttpGet]
    public async Task<ActionResult<ProfileDto>> GetMyProfile()
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        _logger.LogInformation("Fetching profile for UserId={UserId}", userId);
        
        var profile = await _profileService.GetMyProfileAsync(userId);

        if (profile == null)
        {
            _logger.LogWarning("Profile not found for UserId={UserId}", userId);
            return NotFound(new { Message = "Profile not found" });
        }

        return Ok(profile);
    }

    /// <summary>
    /// Get a public user profile by ID
    /// </summary>
    /// <param name="userId">The ID of the user</param>
    /// <returns>Public profile with recent ratings</returns>
    [HttpGet("{userId}")]
    [AllowAnonymous]
    public async Task<ActionResult<PublicProfileDto>> GetUserProfile(int userId)
    {
        _logger.LogInformation("Fetching public profile for UserId={UserId}", userId);

        var profile = await _profileService.GetUserProfileAsync(userId);

        if (profile == null)
        {
            _logger.LogWarning("Public profile not found for UserId={UserId}", userId);
            return NotFound(new { Message = "Profile not found" });
        }

        return Ok(profile);
    }

    [HttpGet("stats")]
    public async Task<ActionResult<ReadingStatsDto>> GetMyStats()
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        _logger.LogInformation("Fetching reading stats for UserId={UserId}", userId);

        var stats = await _profileService.GetReadingStatsAsync(userId);

        if (stats == null)
        {
            _logger.LogWarning("Reading stats not found for UserId={UserId}", userId);
            return NotFound(new { Message = "User not found" });
        }

        return Ok(stats);
    }

    [HttpGet("{userId}/stats")]
    [AllowAnonymous]
    public async Task<ActionResult<ReadingStatsDto>> GetUserStats(int userId)
    {
        _logger.LogInformation("Fetching reading stats for UserId={UserId}", userId);

        var stats = await _profileService.GetReadingStatsAsync(userId);

        if (stats == null)
        {
            _logger.LogWarning("Reading stats not found for UserId={UserId}", userId);
            return NotFound(new { Message = "User not found" });
        }

        return Ok(stats);
    }

    /// <summary>
    /// Follow another user.
    /// </summary>
    [HttpPost("{userId}/follow")]
    public async Task<IActionResult> FollowUser(int userId)
    {
        var followerId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        _logger.LogInformation(
            "Follow request: FollowerId={FollowerId}, FolloweeId={FolloweeId}",
            followerId, userId);

        var (success, error) = await _followService.FollowAsync(followerId, userId);
        if (!success)
        {
            return error switch
            {
                "User not found." => NotFound(new { Message = error }),
                "You cannot follow yourself." => BadRequest(new { Message = error }),
                _ => Conflict(new { Message = error })
            };
        }

        return NoContent();
    }

    /// <summary>
    /// Unfollow a user.
    /// </summary>
    [HttpDelete("{userId}/follow")]
    public async Task<IActionResult> UnfollowUser(int userId)
    {
        var followerId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        _logger.LogInformation(
            "Unfollow request: FollowerId={FollowerId}, FolloweeId={FolloweeId}",
            followerId, userId);

        var (success, error) = await _followService.UnfollowAsync(followerId, userId);
        if (!success)
        {
            return NotFound(new { Message = error });
        }

        return NoContent();
    }

    /// <summary>
    /// Get the list of users following the given user.
    /// </summary>
    [HttpGet("{userId}/followers")]
    [AllowAnonymous]
    public async Task<ActionResult<List<FollowUserDto>>> GetFollowers(int userId)
    {
        var followers = await _followService.GetFollowersAsync(userId);
        if (followers == null)
        {
            return NotFound(new { Message = "User not found" });
        }
        return Ok(followers);
    }

    /// <summary>
    /// Get the list of users the given user is following.
    /// </summary>
    [HttpGet("{userId}/following")]
    [AllowAnonymous]
    public async Task<ActionResult<List<FollowUserDto>>> GetFollowing(int userId)
    {
        var following = await _followService.GetFollowingAsync(userId);
        if (following == null)
        {
            return NotFound(new { Message = "User not found" });
        }
        return Ok(following);
    }

    /// <summary>
    /// Get follower/following counts for a user.
    /// </summary>
    [HttpGet("{userId}/follow-stats")]
    [AllowAnonymous]
    public async Task<ActionResult<FollowStatsDto>> GetFollowStats(int userId)
    {
        var stats = await _followService.GetFollowStatsAsync(userId);
        if (stats == null)
        {
            return NotFound(new { Message = "User not found" });
        }
        return Ok(stats);
    }

    /// <summary>
    /// Get the authenticated user's activity feed (ratings + reading-list additions from followed users).
    /// </summary>
    [HttpGet("feed")]
    public async Task<ActionResult<ActivityFeedDto>> GetFeed(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        _logger.LogInformation(
            "Feed request: UserId={UserId}, Page={Page}, PageSize={PageSize}",
            userId, page, pageSize);

        var feed = await _followService.GetActivityFeedAsync(userId, page, pageSize);
        return Ok(feed);
    }
}
