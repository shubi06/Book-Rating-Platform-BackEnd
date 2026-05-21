using System.Security.Claims;
using BookRatingAPI.DTOs;
using BookRatingAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BookRatingAPI.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class RecommendationsController : ControllerBase
{
    private readonly IRecommendationsService _recommendationsService;
    private readonly ILogger<RecommendationsController> _logger;

    public RecommendationsController(
        IRecommendationsService recommendationsService,
        ILogger<RecommendationsController> logger)
    {
        _recommendationsService = recommendationsService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<List<BookDto>>> GetRecommendations()
    {
        var raw = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(raw, out var userId))
        {
            return Unauthorized(new { Message = "Invalid or missing user identifier." });
        }
        _logger.LogInformation("Fetching recommendations for UserId={UserId}", userId);

        var recommendations = await _recommendationsService.GetRecommendationsAsync(userId);

        _logger.LogInformation(
            "Returning {Count} recommendations for UserId={UserId}", recommendations.Count, userId);

        return Ok(recommendations);
    }
}
