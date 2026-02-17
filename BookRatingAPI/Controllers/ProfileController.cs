using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using BookRatingAPI.DTOs;
using BookRatingAPI.Services;

namespace BookRatingAPI.Controllers;

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

    [HttpGet]
    public async Task<ActionResult<ProfileDto>> GetMyProfile()
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var profile = await _profileService.GetMyProfileAsync(userId);

        if (profile == null)
            return NotFound();

        return Ok(profile);
    }

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
