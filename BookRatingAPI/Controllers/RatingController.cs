using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using BookRatingAPI.DTOs;
using BookRatingAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace BookRatingAPI.Controllers;

/// <summary>
/// Controller for managing book ratings and reviews
/// </summary>
[Authorize]
[ApiController]
[Route("api/[controller]")]
public class RatingsController : ControllerBase
{
    private readonly IRatingService _ratingService;
    private readonly ILogger<RatingsController> _logger;

    public RatingsController(IRatingService ratingService, ILogger<RatingsController> logger)
    {
        _ratingService = ratingService;
        _logger = logger;
    }

    /// <summary>
    /// Get all ratings for a specific book
    /// </summary>
    /// <param name="bookId">The ID of the book</param>
    /// <returns>List of ratings for the book</returns>
    [AllowAnonymous]
    [HttpGet("book/{bookId}")]
    public async Task<ActionResult<IEnumerable<RatingDto>>> GetBookRatings(int bookId)
    {
        _logger.LogInformation("Fetching ratings for BookId={BookId}", bookId);
        var ratings = await _ratingService.GetBookRatingsAsync(bookId);
        _logger.LogInformation("Retrieved {Count} ratings for BookId={BookId}", ratings.Count, bookId);
        return Ok(ratings);
    }

    /// <summary>
    /// Get all ratings created by the authenticated user
    /// </summary>
    /// <returns>List of user's ratings</returns>
    [HttpGet("my")]
    public async Task<ActionResult<IEnumerable<RatingDto>>> GetMyRatings()
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        _logger.LogInformation("Fetching ratings for UserId={UserId}", userId);
        var ratings = await _ratingService.GetUserRatingsAsync(userId);
        _logger.LogInformation("Retrieved {Count} ratings for UserId={UserId}", ratings.Count, userId);
        return Ok(ratings);
    }

    /// <summary>
    /// Create a new rating for a book
    /// </summary>
    /// <param name="dto">Rating creation data</param>
    /// <returns>The created rating</returns>
    [HttpPost]
    public async Task<ActionResult<RatingDto>> CreateRating(CreateRatingDto dto)
    {
        if (!ModelState.IsValid)
        {
            _logger.LogWarning("Invalid model state for rating creation");
            return BadRequest(ModelState);
        }

        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        _logger.LogInformation("Creating rating for BookId={BookId} by UserId={UserId}", dto.BookId, userId);
        
        var rating = await _ratingService.CreateRatingAsync(userId, dto);

        if (rating == null)
        {
            _logger.LogWarning("Failed to create rating: User already rated BookId={BookId}", dto.BookId);
            return BadRequest(new { Message = "You have already rated this book" });
        }

        _logger.LogInformation("Rating created successfully: RatingId={RatingId}", rating.Id);
        return Ok(rating);
    }

    /// <summary>
    /// Update an existing rating
    /// </summary>
    /// <param name="id">The ID of the rating to update</param>
    /// <param name="dto">Updated rating data</param>
    /// <returns>The updated rating</returns>
    [HttpPut("{id}")]
    public async Task<ActionResult<RatingDto>> UpdateRating(int id, CreateRatingDto dto)
    {
        if (!ModelState.IsValid)
        {
            _logger.LogWarning("Invalid model state for rating update: RatingId={RatingId}", id);
            return BadRequest(ModelState);
        }

        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        _logger.LogInformation("Updating rating: RatingId={RatingId} by UserId={UserId}", id, userId);
        
        var rating = await _ratingService.UpdateRatingAsync(id, userId, dto);

        if (rating == null)
        {
            _logger.LogWarning("Rating not found or unauthorized: RatingId={RatingId}, UserId={UserId}", id, userId);
            return NotFound(new { Message = "Rating not found or you don't have permission to update it" });
        }

        _logger.LogInformation("Rating updated successfully: RatingId={RatingId}", id);
        return Ok(rating);
    }

    /// <summary>
    /// Delete a rating
    /// </summary>
    /// <param name="id">The ID of the rating to delete</param>
    /// <returns>No content on success</returns>
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteRating(int id)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        _logger.LogInformation("Deleting rating: RatingId={RatingId} by UserId={UserId}", id, userId);
        
        var deleted = await _ratingService.DeleteRatingAsync(id, userId);

        if (!deleted)
        {
            _logger.LogWarning("Rating not found or unauthorized: RatingId={RatingId}, UserId={UserId}", id, userId);
            return NotFound(new { Message = "Rating not found or you don't have permission to delete it" });
        }

        _logger.LogInformation("Rating deleted successfully: RatingId={RatingId}", id);
        return NoContent();
    }
}
