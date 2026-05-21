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
