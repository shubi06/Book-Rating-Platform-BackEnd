using BookRatingAPI.Data;
using BookRatingAPI.DTOs;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace BookRatingAPI.Services
{
    public class RecommendationsService : IRecommendationsService
    {
        private readonly AppDbContext _context;
        private readonly IMemoryCache _cache;
        private readonly ILogger<RecommendationsService> _logger;

        private const int TopCategoryCount = 3;
        private const int RecommendationLimit = 10;
        private const int CacheTtlMinutes = 2;
        private const int SocialMinScore = 4;

        public RecommendationsService(
            AppDbContext context,
            IMemoryCache cache,
            ILogger<RecommendationsService> logger)
        {
            _context = context;
            _cache = cache;
            _logger = logger;
        }

        public async Task<List<BookDto>> GetRecommendationsAsync(int userId)
        {
            var cacheKey = $"recommendations:{userId}";

            if (_cache.TryGetValue(cacheKey, out List<BookDto>? cached) && cached != null)
            {
                _logger.LogInformation("Returning cached recommendations for UserId={UserId}", userId);
                return cached;
            }

            var hasRatings = await _context.Ratings.AnyAsync(r => r.UserId == userId);

            List<BookDto> result;

            if (!hasRatings)
            {
                _logger.LogInformation("Cold-start recommendations for UserId={UserId}", userId);
                result = await GetColdStartRecommendationsAsync();
            }
            else
            {
                _logger.LogInformation("Personalised recommendations for UserId={UserId}", userId);
                result = await GetPersonalisedRecommendationsAsync(userId);
            }

            _cache.Set(cacheKey, result, TimeSpan.FromMinutes(CacheTtlMinutes));
            _logger.LogInformation(
                "Cached {Count} recommendations for UserId={UserId}", result.Count, userId);

            return result;
        }

        private async Task<List<BookDto>> GetPersonalisedRecommendationsAsync(int userId)
        {
            var topCategoryIds = await _context.Ratings
                .Where(r => r.UserId == userId)
                .GroupBy(r => r.Book.CategoryId)
                .Select(g => new { CategoryId = g.Key, AvgScore = g.Average(r => r.Score) })
                .OrderByDescending(x => x.AvgScore)
                .Take(TopCategoryCount)
                .Select(x => x.CategoryId)
                .ToListAsync();

            return await _context.Books
                .Where(b =>
                    topCategoryIds.Contains(b.CategoryId) &&
                    !_context.Ratings.Any(r => r.UserId == userId && r.BookId == b.Id))
                .Select(b => new
                {
                    b.Id,
                    b.Title,
                    b.Author,
                    b.Description,
                    b.CoverImageUrl,
                    b.PublicationYear,
                    b.ISBN,
                    b.CategoryId,
                    CategoryName = b.Category.Name,
                    AvgRating = b.Ratings.Any()
                        ? b.Ratings.Average(r => (double)r.Score)
                        : 0.0,
                    RatingCount = b.Ratings.Count(),
                })
                .OrderByDescending(x => x.AvgRating)
                .Take(RecommendationLimit)
                .Select(x => new BookDto
                {
                    Id = x.Id,
                    Title = x.Title,
                    Author = x.Author,
                    Description = x.Description,
                    CoverImageUrl = x.CoverImageUrl,
                    PublicationYear = x.PublicationYear,
                    ISBN = x.ISBN,
                    CategoryId = x.CategoryId,
                    CategoryName = x.CategoryName,
                    AverageRating = x.AvgRating,
                    RatingCount = x.RatingCount,
                })
                .ToListAsync();
        }

        public async Task<List<BookDto>> GetSocialRecommendationsAsync(int userId)
        {
            var cacheKey = $"recommendations:social:{userId}";

            if (_cache.TryGetValue(cacheKey, out List<BookDto>? cached) && cached != null)
            {
                _logger.LogInformation(
                    "Returning cached social recommendations for UserId={UserId}", userId);
                return cached;
            }

            var followeeIds = await _context.Follows
                .Where(f => f.FollowerId == userId)
                .Select(f => f.FolloweeId)
                .ToListAsync();

            List<BookDto> result;

            if (followeeIds.Count == 0)
            {
                _logger.LogInformation(
                    "No follows for UserId={UserId}; returning empty social recommendations", userId);
                result = new List<BookDto>();
            }
            else
            {
                var bookLatest = await _context.Ratings
                    .Where(r =>
                        followeeIds.Contains(r.UserId)
                        && r.Score >= SocialMinScore
                        && !_context.Ratings.Any(rr => rr.UserId == userId && rr.BookId == r.BookId)
                        && !_context.ReadingLists.Any(rl => rl.UserId == userId && rl.BookId == r.BookId))
                    .GroupBy(r => r.BookId)
                    .Select(g => new { BookId = g.Key, LatestRatedAt = g.Max(r => r.CreatedAt) })
                    .OrderByDescending(x => x.LatestRatedAt)
                    .Take(RecommendationLimit)
                    .ToListAsync();

                var topBookIds = bookLatest.Select(x => x.BookId).ToList();

                var books = await _context.Books
                    .Where(b => topBookIds.Contains(b.Id))
                    .Select(b => new
                    {
                        b.Id,
                        b.Title,
                        b.Author,
                        b.Description,
                        b.CoverImageUrl,
                        b.PublicationYear,
                        b.ISBN,
                        b.CategoryId,
                        CategoryName = b.Category.Name,
                        AvgRating = b.Ratings.Any()
                            ? b.Ratings.Average(r => (double)r.Score)
                            : 0.0,
                        RatingCount = b.Ratings.Count(),
                    })
                    .ToListAsync();

                var orderIndex = bookLatest
                    .Select((x, i) => new { x.BookId, Index = i })
                    .ToDictionary(x => x.BookId, x => x.Index);

                result = books
                    .OrderBy(b => orderIndex[b.Id])
                    .Select(b => new BookDto
                    {
                        Id = b.Id,
                        Title = b.Title,
                        Author = b.Author,
                        Description = b.Description,
                        CoverImageUrl = b.CoverImageUrl,
                        PublicationYear = b.PublicationYear,
                        ISBN = b.ISBN,
                        CategoryId = b.CategoryId,
                        CategoryName = b.CategoryName,
                        AverageRating = b.AvgRating,
                        RatingCount = b.RatingCount,
                    })
                    .ToList();
            }

            _cache.Set(cacheKey, result, TimeSpan.FromMinutes(CacheTtlMinutes));
            _logger.LogInformation(
                "Cached {Count} social recommendations for UserId={UserId}", result.Count, userId);

            return result;
        }

        private async Task<List<BookDto>> GetColdStartRecommendationsAsync()
        {
            return await _context.Books
                .Select(b => new
                {
                    b.Id,
                    b.Title,
                    b.Author,
                    b.Description,
                    b.CoverImageUrl,
                    b.PublicationYear,
                    b.ISBN,
                    b.CategoryId,
                    CategoryName = b.Category.Name,
                    AvgRating = b.Ratings.Any()
                        ? b.Ratings.Average(r => (double)r.Score)
                        : 0.0,
                    RatingCount = b.Ratings.Count(),
                })
                .OrderByDescending(x => x.AvgRating)
                .Take(RecommendationLimit)
                .Select(x => new BookDto
                {
                    Id = x.Id,
                    Title = x.Title,
                    Author = x.Author,
                    Description = x.Description,
                    CoverImageUrl = x.CoverImageUrl,
                    PublicationYear = x.PublicationYear,
                    ISBN = x.ISBN,
                    CategoryId = x.CategoryId,
                    CategoryName = x.CategoryName,
                    AverageRating = x.AvgRating,
                    RatingCount = x.RatingCount,
                })
                .ToListAsync();
        }
    }
}
