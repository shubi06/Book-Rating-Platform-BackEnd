using System.Security.Claims;
using System.Threading.Tasks;
using BookRatingAPI.DTOs;
using BookRatingAPI.DTOs.ProfileDTOs;
using BookRatingAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

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

    public ProfileController(IProfileService profileService)
    {
        _profileService = profileService;
    }

    /// <summary>
    /// Get the authenticated user's profile
    /// </summary>
    /// <returns>User profile with statistics</returns>
    [HttpGet]
    public async Task<ActionResult<ProfileDto>> GetMyProfile()
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var profile = await _profileService.GetMyProfileAsync(userId);

        if (profile == null)
            return NotFound();

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
        var profile = await _profileService.GetUserProfileAsync(userId);

        if (profile == null)
            return NotFound();

        return Ok(profile);
    }
}
