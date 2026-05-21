using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using BookRatingAPI.Data;
using BookRatingAPI.DTOs;
using BookRatingAPI.DTOs.ProfileDTOs;
using BookRatingAPI.Models.Enums;

namespace BookRatingAPI.Services;

public class ProfileService : IProfileService
{
    private readonly AppDbContext _context;
    private readonly ILogger<ProfileService> _logger;

    public ProfileService(AppDbContext context, ILogger<ProfileService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<ProfileDto?> GetMyProfileAsync(int userId)
    {
        _logger.LogInformation("Fetching profile for UserId={UserId}", userId);
        
        var user = await _context.Users
            .Include(u => u.Ratings)
            .Include(u => u.ReadingLists)
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (user == null)
        {
            _logger.LogWarning("User not found: UserId={UserId}", userId);
            return null;
        }

        _logger.LogInformation("Profile retrieved successfully for UserId={UserId}", userId);
        return new ProfileDto
        {
            Id = user.Id,
            Username = user.Username,
            Email = user.Email,
            CreatedAt = user.CreatedAt,
            Stats = new ProfileStatsDto
            {
                TotalRatings = user.Ratings.Count,
                //Calculate average score; default 0 if no ratings exist
                AverageRating = user.Ratings.Any() ? user.Ratings.Average(r => r.Score) : 0,
                BooksInReadingList = user.ReadingLists.Count
            }
        };
    }

    public async Task<PublicProfileDto?> GetUserProfileAsync(int userId)
    {
        _logger.LogInformation("Fetching public profile for UserId={UserId}", userId);
        
        var user = await _context.Users
            .Include(u => u.Ratings)
            .ThenInclude(r => r.Book)
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (user == null)
        {
            _logger.LogWarning("User not found for public profile: UserId={UserId}", userId);
            return null;
        }

        _logger.LogInformation("Public profile retrieved successfully for UserId={UserId}", userId);
        return new PublicProfileDto
        {
            Id = user.Id,
            Username = user.Username,
            CreatedAt = user.CreatedAt,
            Stats = new ProfileStatsDto
            {
                TotalRatings = user.Ratings.Count,
                AverageRating = user.Ratings.Any() ? user.Ratings.Average(r => r.Score) : 0,
                BooksInReadingList = 0
            },
            //Map only the 5 most recent ratings
            RecentRatings = user.Ratings
                .OrderByDescending(r => r.CreatedAt)
                .Take(5)
                .Select(r => new RecentRatingDto
                {
                    Id = r.Id,
                    Score = r.Score,
                    Comment = r.Comment,
                    CreatedAt = r.CreatedAt,
                    Book = new ProfileBookDto
                    {
                        Id = r.Book.Id,
                        Title = r.Book.Title,
                        Author = r.Book.Author
                    }
                })
                .ToList()
        };
    }

    public async Task<ReadingStatsDto?> GetReadingStatsAsync(int userId)
    {
        _logger.LogInformation("Fetching reading stats for UserId={UserId}", userId);

        var userExists = await _context.Users.AnyAsync(u => u.Id == userId);
        if (!userExists)
        {
            _logger.LogWarning("User not found for reading stats: UserId={UserId}", userId);
            return null;
        }

        // Counts pushed to SQL — no row materialization.
        var booksRead = await _context.ReadingLists
            .CountAsync(rl => rl.UserId == userId && rl.Status == ReadingStatus.Read);

        var booksWantToRead = await _context.ReadingLists
            .CountAsync(rl => rl.UserId == userId && rl.Status == ReadingStatus.WantToRead);

        // Cast to double? so AverageAsync returns null instead of throwing when there are no ratings.
        double? averageRatingGiven = await _context.Ratings
            .Where(r => r.UserId == userId)
            .Select(r => (double?)r.Score)
            .AverageAsync();

        // Favorite category: combine activity from ratings and reading-list entries per category.
        // Grouped in SQL on both sides; only the small per-category aggregate set comes back.
        var ratingCategoryCounts = await _context.Ratings
            .Where(r => r.UserId == userId)
            .GroupBy(r => r.Book.Category.Name)
            .Select(g => new { Name = g.Key, Count = g.Count() })
            .ToListAsync();

        var readingCategoryCounts = await _context.ReadingLists
            .Where(rl => rl.UserId == userId)
            .GroupBy(rl => rl.Book.Category.Name)
            .Select(g => new { Name = g.Key, Count = g.Count() })
            .ToListAsync();

        string? favoriteCategory = ratingCategoryCounts
            .Concat(readingCategoryCounts)
            .GroupBy(x => x.Name)
            .Select(g => new { Name = g.Key, Total = g.Sum(x => x.Count) })
            .OrderByDescending(x => x.Total)
            .ThenBy(x => x.Name)
            .FirstOrDefault()?.Name;

        // Most active month: group ratings by (year, month) in SQL; format and tie-break in memory.
        // ThenBy(Month) ensures the lexicographically earlier month wins on a tie for stable output.
        var monthCounts = await _context.Ratings
            .Where(r => r.UserId == userId)
            .GroupBy(r => new { r.CreatedAt.Year, r.CreatedAt.Month })
            .Select(g => new { g.Key.Year, g.Key.Month, Count = g.Count() })
            .ToListAsync();

        string? mostActiveMonth = monthCounts
            .Select(m => new { Month = $"{m.Year:D4}-{m.Month:D2}", m.Count })
            .OrderByDescending(x => x.Count)
            .ThenBy(x => x.Month)
            .FirstOrDefault()?.Month;

        _logger.LogInformation(
            "Reading stats retrieved for UserId={UserId}: BooksRead={BooksRead}, BooksWantToRead={BooksWantToRead}",
            userId, booksRead, booksWantToRead);

        return new ReadingStatsDto
        {
            BooksRead = booksRead,
            BooksWantToRead = booksWantToRead,
            AverageRatingGiven = averageRatingGiven,
            FavoriteCategory = favoriteCategory,
            MostActiveMonth = mostActiveMonth
        };
    }
}
