using BookRatingAPI.Data;
using BookRatingAPI.DTOs;
using BookRatingAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace BookRatingAPI.Services;

/// <summary>
/// Service for managing book ratings and reviews
/// </summary>
public class RatingService : IRatingService
{
    private readonly AppDbContext _context;
    private readonly IElasticSearchService _elastic;

    public RatingService(AppDbContext context, IElasticSearchService elastic)
    {
        _context = context;
        _elastic = elastic;
    }

    /// <summary>
    /// Get all ratings for a specific book
    /// </summary>
    /// <param name="bookId">The ID of the book</param>
    /// <returns>List of ratings with user information</returns>
    public async Task<List<RatingDto>> GetBookRatingsAsync(int bookId)
    {
        return await _context
            .Ratings.Include(r => r.User)
            .Where(r => r.BookId == bookId)
            .Select(r => MapToDto(r))
            .ToListAsync();
    }

    /// <summary>
    /// Get all ratings created by a specific user
    /// </summary>
    /// <param name="userId">The ID of the user</param>
    /// <returns>List of user's ratings</returns>
    public async Task<List<RatingDto>> GetUserRatingsAsync(int userId)
    {
        return await _context
            .Ratings.Include(r => r.User)
            .Include(r => r.Book)
            .Where(r => r.UserId == userId)
            .Select(r => MapToDto(r))
            .ToListAsync();
    }

    /// <summary>
    /// Create a new rating for a book
    /// </summary>
    /// <param name="userId">The ID of the user creating the rating</param>
    /// <param name="dto">Rating creation data</param>
    /// <returns>The created rating, or null if user already rated this book</returns>
    public async Task<RatingDto?> CreateRatingAsync(int userId, CreateRatingDto dto)
    {
        // Check if user already rated this book
        var existing = await _context.Ratings.FirstOrDefaultAsync(r =>
            r.UserId == userId && r.BookId == dto.BookId
        );

        if (existing != null)
            return null;

        var rating = new Rating
        {
            UserId = userId,
            BookId = dto.BookId,
            Score = dto.Score,
            Comment = dto.Comment,
        };

        _context.Ratings.Add(rating);
        await _context.SaveChangesAsync();
        await _elastic.UpsertBook(rating.BookId);

        // Load user information for DTO mapping
        var user = await _context.Users.FindAsync(userId);
        rating.User = user!;

        return MapToDto(rating);
    }

    /// <summary>
    /// Update an existing rating
    /// </summary>
    /// <param name="id">The ID of the rating to update</param>
    /// <param name="userId">The ID of the user (for authorization)</param>
    /// <param name="dto">Updated rating data</param>
    /// <returns>The updated rating, or null if not found or unauthorized</returns>
    public async Task<RatingDto?> UpdateRatingAsync(int id, int userId, CreateRatingDto dto)
    {
        var rating = await _context.Ratings.FindAsync(id);

        // Verify rating exists and belongs to the user
        if (rating == null || rating.UserId != userId)
            return null;

        rating.Score = dto.Score;
        rating.Comment = dto.Comment;
        rating.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        await _elastic.UpsertBook(rating.BookId);

        // Load user information for DTO mapping
        var user = await _context.Users.FindAsync(userId);
        rating.User = user!;

        return MapToDto(rating);
    }

    /// <summary>
    /// Delete a rating
    /// </summary>
    /// <param name="id">The ID of the rating to delete</param>
    /// <param name="userId">The ID of the user (for authorization)</param>
    /// <returns>True if deleted successfully, false if not found or unauthorized</returns>
    public async Task<bool> DeleteRatingAsync(int id, int userId)
    {
        var rating = await _context.Ratings.FindAsync(id);

        // Verify rating exists and belongs to the user
        if (rating == null || rating.UserId != userId)
            return false;

        _context.Ratings.Remove(rating);
        await _context.SaveChangesAsync();

        return true;
    }

    /// <summary>
    /// Maps a Rating entity to a RatingDto
    /// </summary>
    /// <param name="rating">The rating entity</param>
    /// <returns>The mapped DTO</returns>
    private static RatingDto MapToDto(Rating rating)
    {
        return new RatingDto
        {
            Id = rating.Id,
            BookId = rating.BookId,
            UserId = rating.UserId,
            Username = rating.User.Username,
            Score = rating.Score,
            Comment = rating.Comment,
            CreatedAt = rating.CreatedAt,
        };
    }
}
