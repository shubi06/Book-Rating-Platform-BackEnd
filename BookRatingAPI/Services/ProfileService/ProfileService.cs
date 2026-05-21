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

        var ratings = await _context.Ratings
            .Include(r => r.Book)
            .ThenInclude(b => b.Category)
            .Where(r => r.UserId == userId)
            .ToListAsync();

        var readingEntries = await _context.ReadingLists
            .Include(rl => rl.Book)
            .ThenInclude(b => b.Category)
            .Where(rl => rl.UserId == userId)
            .ToListAsync();

        var booksRead = readingEntries.Count(rl => rl.Status == ReadingStatus.Read);
        var booksWantToRead = readingEntries.Count(rl => rl.Status == ReadingStatus.WantToRead);

        double? averageRatingGiven = ratings.Count > 0
            ? ratings.Average(r => r.Score)
            : null;

        // Merge category activity: count of ratings + count of reading list entries per category name.
        var categoryCounts = ratings
            .Select(r => r.Book.Category.Name)
            .Concat(readingEntries.Select(rl => rl.Book.Category.Name))
            .GroupBy(name => name)
            .Select(g => new { Category = g.Key, Count = g.Count() })
            .OrderByDescending(x => x.Count)
            .ToList();

        string? favoriteCategory = categoryCounts.Count > 0
            ? categoryCounts[0].Category
            : null;

        // Most active month: the "yyyy-MM" string with the most ratings submitted.
        // ThenBy ensures the lexicographically earlier month wins on a tie (consistent tie-breaking).
        var monthCounts = ratings
            .GroupBy(r => r.CreatedAt.ToString("yyyy-MM"))
            .Select(g => new { Month = g.Key, Count = g.Count() })
            .OrderByDescending(x => x.Count)
            .ThenBy(x => x.Month)
            .ToList();

        string? mostActiveMonth = monthCounts.Count > 0
            ? monthCounts[0].Month
            : null;

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
