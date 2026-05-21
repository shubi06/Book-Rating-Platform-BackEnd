using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BookRatingAPI.Data;
using BookRatingAPI.DTOs;
using BookRatingAPI.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BookRatingAPI.Services;

public class RatingService : IRatingService
{
    private readonly AppDbContext _context;
    private readonly ILogger<RatingService> _logger;
    private readonly IBookSyncService _sync;

    public RatingService(AppDbContext context, ILogger<RatingService> logger, IBookSyncService sync)
    {
        _context = context;
        _logger = logger;
        _sync = sync;
    }

    public async Task<List<RatingDto>> GetBookRatingsAsync(int bookId)
    {
        _logger.LogInformation("Fetching ratings for BookId={BookId}", bookId);
        var ratings = await _context.Ratings
            .Include(r => r.User)
            .Include(r => r.Book)
            .Where(r => r.BookId == bookId)
            .ToListAsync();
        
        var result = ratings.Select(r => MapToDto(r)).ToList();
        _logger.LogInformation("Retrieved {Count} ratings for BookId={BookId}", result.Count, bookId);
        return result;
    }

    public async Task<List<RatingDto>> GetUserRatingsAsync(int userId)
    {
        _logger.LogInformation("Fetching ratings for UserId={UserId}", userId);
        var ratings = await _context.Ratings
            .Include(r => r.User)
            .Include(r => r.Book)
            .Where(r => r.UserId == userId)
            .ToListAsync();
        
        var result = ratings.Select(r => MapToDto(r)).ToList();
        _logger.LogInformation("Retrieved {Count} ratings for UserId={UserId}", result.Count, userId);
        return result;
    }

    public async Task<RatingDto?> CreateRatingAsync(int userId, CreateRatingDto dto)
    {
        _logger.LogInformation("Creating rating for BookId={BookId} by UserId={UserId}", dto.BookId, userId);
        
        var existing = await _context.Ratings
            .FirstOrDefaultAsync(r => r.UserId == userId && r.BookId == dto.BookId);

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
            Comment = dto.Comment
        };

        _context.Ratings.Add(rating);
        await _context.SaveChangesAsync();
        await _sync.SyncBookAsync(rating.BookId);
        _sync.InvalidateUserRecommendations(userId);

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
        _sync.InvalidateUserRecommendations(userId);

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
        _sync.InvalidateUserRecommendations(userId);

        _logger.LogInformation("Rating deleted successfully: RatingId={RatingId}", id);
        return true;
    }

    private static RatingDto MapToDto(Rating rating)
    {
        return new RatingDto
        {
            Id = rating.Id,
            BookId = rating.BookId,
            BookTitle = rating.Book?.Title ?? "Unknown",
            UserId = rating.UserId,
            Username = rating.User?.Username ?? "Unknown",
            Score = rating.Score,
            Comment = rating.Comment,
            CreatedAt = rating.CreatedAt,
        };
    }
}
