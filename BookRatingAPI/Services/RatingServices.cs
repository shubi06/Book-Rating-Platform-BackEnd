using Microsoft.EntityFrameworkCore;
using BookRatingAPI.Data;
using BookRatingAPI.Models;

namespace BookRatingAPI.Services;

public class RatingService
{
	private readonly AppDbContext _context;

	public RatingService(AppDbContext context)
	{
		_context = context;
	}

	public async Task<List<DTOs.RatingDto>> GetBookRatingsAsync(int bookId)
	{
		return await _context.Ratings
			.Include(r => r.User)
			.Where(r => r.BookId == bookId)
			.Select(r => MapToDto(r))
			.ToListAsync();
	}

	public async Task<List<DTOs.RatingDto>> GetUserRatingsAsync(int userId)
	{
		return await _context.Ratings
			.Include(r => r.User)
			.Where(r => r.UserId == userId)
			.Select(r => MapToDto(r))
			.ToListAsync();
	}

	public async Task<DTOs.RatingDto?> CreateRatingAsync(int userId, CreateRatingDto dto)
	{
		var existing = await _context.Ratings
			.FirstOrDefaultAsync(r => r.UserId == userId && r.BookId == dto.BookId);

		if (existing != null)
			return null;

		var rating = new Rating
		{
			UserId = userId,
			BookId = dto.BookId,
			Score = dto.Score,
			Comment = dto.Comment
		};

		_context.Ratings.Add(rating);
		await _context.SaveChangesAsync();

		var user = await _context.Users.FindAsync(userId);
		rating.User = user!;

		return MapToDto(rating);
	}

	public async Task<DTOs.RatingDto?> UpdateRatingAsync(int id, int userId, CreateRatingDto dto)
	{
		var rating = await _context.Ratings.FindAsync(id);

		if (rating == null || rating.UserId != userId)
			return null;

		rating.Score = dto.Score;
		rating.Comment = dto.Comment;
		rating.UpdatedAt = DateTime.UtcNow;

		await _context.SaveChangesAsync();

		var user = await _context.Users.FindAsync(userId);
		rating.User = user!;

		return MapToDto(rating);
	}

	public async Task<bool> DeleteRatingAsync(int id, int userId)
	{
		var rating = await _context.Ratings.FindAsync(id);

		if (rating == null || rating.UserId != userId)
			return false;

		_context.Ratings.Remove(rating);
		await _context.SaveChangesAsync();

		return true;
	}

	private static DTOs.RatingDto MapToDto(Rating rating)
	{
		return new DTOs.RatingDto
		{
			Id = rating.Id,
			BookId = rating.BookId,
			UserId = rating.UserId,
			Username = rating.User.Username,
			Score = rating.Score,
			Comment = rating.Comment,
			CreatedAt = rating.CreatedAt
		};
	}
}
