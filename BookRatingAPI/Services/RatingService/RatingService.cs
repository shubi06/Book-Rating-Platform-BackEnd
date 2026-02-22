using BookRatingAPI.Data;
using BookRatingAPI.DTOs;
using BookRatingAPI.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BookRatingAPI.Services;

public class RatingService : IRatingService
{
    private readonly AppDbContext _context;
    private readonly IBookSyncService _sync;

    public RatingService(AppDbContext context, IBookSyncService sync)
    {
        _context = context;
        _sync = sync;
    }

    public async Task<List<RatingDto>> GetBookRatingsAsync(int bookId)
    {
        return await _context
            .Ratings.Include(r => r.User)
            .Where(r => r.BookId == bookId)
            .Select(r => MapToDto(r))
            .ToListAsync();
        _logger.LogInformation("Retrieved {Count} ratings for BookId={BookId}", ratings.Count, bookId);
        return ratings;
    }

    public async Task<List<RatingDto>> GetUserRatingsAsync(int userId)
    {
        return await _context
            .Ratings.Include(r => r.User)
            .Include(r => r.Book)
            .Where(r => r.UserId == userId)
            .Select(r => MapToDto(r))
            .ToListAsync();
        _logger.LogInformation("Retrieved {Count} ratings for UserId={UserId}", ratings.Count, userId);
        return ratings;
    }

    public async Task<RatingDto?> CreateRatingAsync(int userId, CreateRatingDto dto)
    {
        // Check if user already rated this book
        var existing = await _context.Ratings.FirstOrDefaultAsync(r =>
            r.UserId == userId && r.BookId == dto.BookId
        );

        if (existing != null)
        {
            _logger.LogWarning("User already rated this book: UserId={UserId}, BookId={BookId}", userId, dto.BookId);
            return null;
        }

        var rating = new Rating
        {
            UserId = userId,
            BookId = dto.BookId,
            Score = dto.Score,
            Comment = dto.Comment,
        };

        _context.Ratings.Add(rating);
        await _context.SaveChangesAsync();
        await _sync.SyncBookAsync(rating.BookId);

        var user = await _context.Users.FindAsync(userId);
        rating.User = user!;

        _logger.LogInformation("Rating created successfully: RatingId={RatingId}", rating.Id);
        return MapToDto(rating);
    }

    public async Task<RatingDto?> UpdateRatingAsync(int id, int userId, CreateRatingDto dto)
    {
        _logger.LogInformation("Updating rating: RatingId={RatingId} by UserId={UserId}", id, userId);
        
        var rating = await _context.Ratings.FindAsync(id);

        if (rating == null || rating.UserId != userId)
        {
            _logger.LogWarning("Rating not found or unauthorized: RatingId={RatingId}, UserId={UserId}", id, userId);
            return null;
        }

        rating.Score = dto.Score;
        rating.Comment = dto.Comment;

        await _context.SaveChangesAsync();
        await _sync.SyncBookAsync(rating.BookId);

        var user = await _context.Users.FindAsync(userId);
        rating.User = user!;

        _logger.LogInformation("Rating updated successfully: RatingId={RatingId}", id);
        return MapToDto(rating);
    }

    public async Task<bool> DeleteRatingAsync(int id, int userId)
    {
        _logger.LogInformation("Deleting rating: RatingId={RatingId} by UserId={UserId}", id, userId);
        
        var rating = await _context.Ratings.FindAsync(id);

        if (rating == null || rating.UserId != userId)
        {
            _logger.LogWarning("Rating not found or unauthorized: RatingId={RatingId}, UserId={UserId}", id, userId);
            return false;
        }

        _context.Ratings.Remove(rating);
        await _context.SaveChangesAsync();
        await _sync.RemoveBookAsync(rating.BookId);

        _logger.LogInformation("Rating deleted successfully: RatingId={RatingId}", id);
        return true;
    }

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
