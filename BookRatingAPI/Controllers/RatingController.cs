using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using BookRatingAPI.DTOs;
using BookRatingAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

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

    public RatingsController(IRatingService ratingService)
    {
        _ratingService = ratingService;
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
        var ratings = await _ratingService.GetBookRatingsAsync(bookId);
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
        var ratings = await _ratingService.GetUserRatingsAsync(userId);
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
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var rating = await _ratingService.CreateRatingAsync(userId, dto);

        if (rating == null)
            return BadRequest("You have already rated this book");

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
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var rating = await _ratingService.UpdateRatingAsync(id, userId, dto);

        if (rating == null)
            return NotFound();

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
        var deleted = await _ratingService.DeleteRatingAsync(id, userId);

        if (!deleted)
            return NotFound();

        return NoContent();
    }
}
