using BookRatingAPI.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace BookRatingAPI.Services
{
    public class BookSyncService : IBookSyncService
    {
        private readonly IMemoryCache _cache;
        private readonly IElasticSearchService _elastic;
        private readonly AppDbContext _context;
        private readonly ILogger<BookSyncService> _logger;

        public BookSyncService(
            IMemoryCache cache,
            IElasticSearchService elastic,
            AppDbContext context,
            ILogger<BookSyncService> logger)
        {
            _cache = cache;
            _elastic = elastic;
            _context = context;
            _logger = logger;
        }

        public async Task SyncBookAsync(int bookId)
        {
            // Invalidate cache
            _cache.Remove($"book:{bookId}");
            _cache.Remove("books:all");

            // Reindex Elasticsearch
            await _elastic.UpsertBook(bookId);
        }

        public async Task RemoveBookAsync(int bookId)
        {
            _cache.Remove($"book:{bookId}");
            _cache.Remove("books:all");

            await _elastic.DeleteBook(bookId);
        }

        public void InvalidateUserRecommendations(int userId)
        {
            _cache.Remove($"recommendations:{userId}");
        }

        public async Task InvalidateSocialRecommendationsForFollowersOfAsync(int userId)
        {
            _logger.LogInformation(
                "Invalidating social recommendations for followers of UserId={UserId}", userId);

            var followerIds = await _context.Follows
                .Where(f => f.FolloweeId == userId)
                .Select(f => f.FollowerId)
                .ToListAsync();

            foreach (var followerId in followerIds)
            {
                _cache.Remove($"recommendations:social:{followerId}");
            }

            _logger.LogInformation(
                "Invalidated social recommendation cache for {FollowerCount} followers of UserId={UserId}",
                followerIds.Count,
                userId);
        }
    }
}
