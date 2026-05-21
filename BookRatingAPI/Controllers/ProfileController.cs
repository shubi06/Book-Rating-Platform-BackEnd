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

    // Feed pagination limits enforced at the HTTP boundary. Kept generous;
    // the service applies its own (stricter) MaxPageSize as a final clamp.
    private const int MaxFeedPageSize = 100;

    public ProfileController(
        IProfileService profileService,
        IFollowService followService,
        ILogger<ProfileController> logger)
    {
        _profileService = profileService;
        _followService = followService;
        _logger = logger;
    }

    // JWT validation guarantees the token is valid but not that NameIdentifier
    // is present or numeric, so we guard rather than int.Parse(...!).
    private bool TryGetAuthenticatedUserId(out int userId)
    {
        var raw = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(raw, out userId);
    }

    /// <summary>
    /// Get the authenticated user's profile
    /// </summary>
    /// <returns>User profile with statistics</returns>
    [HttpGet]
    public async Task<ActionResult<ProfileDto>> GetMyProfile()
    {
        if (!TryGetAuthenticatedUserId(out var userId))
        {
            return Unauthorized(new { Message = "Invalid or missing user identifier." });
        }
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
        if (!TryGetAuthenticatedUserId(out var userId))
        {
            return Unauthorized(new { Message = "Invalid or missing user identifier." });
        }
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
        if (!TryGetAuthenticatedUserId(out var followerId))
        {
            return Unauthorized(new { Message = "Invalid or missing user identifier." });
        }
        _logger.LogInformation(
            "Follow request: FollowerId={FollowerId}, FolloweeId={FolloweeId}",
            followerId, userId);

        var result = await _followService.FollowAsync(followerId, userId);
        return result switch
        {
            FollowOperationResult.Success => NoContent(),
            FollowOperationResult.SelfFollow => BadRequest(new { Message = "You cannot follow yourself." }),
            FollowOperationResult.UserNotFound => NotFound(new { Message = "User not found." }),
            FollowOperationResult.AlreadyFollowing => Conflict(new { Message = "You are already following this user." }),
            _ => StatusCode(500, new { Message = "Unexpected follow result." })
        };
    }

    /// <summary>
    /// Unfollow a user.
    /// </summary>
    [HttpDelete("{userId}/follow")]
    public async Task<IActionResult> UnfollowUser(int userId)
    {
        if (!TryGetAuthenticatedUserId(out var followerId))
        {
            return Unauthorized(new { Message = "Invalid or missing user identifier." });
        }
        _logger.LogInformation(
            "Unfollow request: FollowerId={FollowerId}, FolloweeId={FolloweeId}",
            followerId, userId);

        var result = await _followService.UnfollowAsync(followerId, userId);
        return result switch
        {
            FollowOperationResult.Success => NoContent(),
            FollowOperationResult.NotFollowing => NotFound(new { Message = "You are not following this user." }),
            _ => StatusCode(500, new { Message = "Unexpected unfollow result." })
        };
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
        if (page < 1 || pageSize < 1 || pageSize > MaxFeedPageSize)
        {
            return BadRequest(new
            {
                Message = $"page must be >= 1 and pageSize must be between 1 and {MaxFeedPageSize}."
            });
        }

        if (!TryGetAuthenticatedUserId(out var userId))
        {
            return Unauthorized(new { Message = "Invalid or missing user identifier." });
        }

        _logger.LogInformation(
            "Feed request: UserId={UserId}, Page={Page}, PageSize={PageSize}",
            userId, page, pageSize);

        var feed = await _followService.GetActivityFeedAsync(userId, page, pageSize);
        return Ok(feed);
    }
}
