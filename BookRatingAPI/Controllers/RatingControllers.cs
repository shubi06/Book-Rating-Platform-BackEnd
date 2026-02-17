using System.Security.Claims;
using BookRatingAPI.Data;
using BookRatingAPI.DTOs.AuthDTOs;
using BookRatingAPI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class RatingsController : ControllerBase
{
	private readonly AppDbContext _context;

	public RatingsController(AppDbContext context)
	{
		_context = context;
	}

	[HttpPost]
	public async Task<IActionResult> CreateRating(CreateRatingDto dto)
	{
		// Convert JWT string -> int
		var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
		int userId = int.Parse(userIdString);

		// Check if user already rated
		var existing = await _context.Ratings
			.FirstOrDefaultAsync(x => x.UserId == userId && x.BookId == dto.BookId);

		if (existing != null)
			return BadRequest("You already rated this book.");

		var rating = new Rating
		{
			BookId = dto.BookId,
			UserId = userId,
			Stars = dto.Stars,
			Comment = dto.Comment
		};

		_context.Ratings.Add(rating);
		await _context.SaveChangesAsync();

		await UpdateBookAverage(dto.BookId);

		return Ok(new { message = "Rating submitted successfully!" });
	}

	private async Task UpdateBookAverage(int bookId)
	{
		var ratings = await _context.Ratings
			.Where(r => r.BookId == bookId)
			.ToListAsync();

		var book = await _context.Books.FindAsync(bookId);

		if (book == null) return;

		book.AverageRating = ratings.Average(r => r.Stars);

		await _context.SaveChangesAsync();
	}
}
