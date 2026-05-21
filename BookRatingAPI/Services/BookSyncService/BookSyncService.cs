using BookRatingAPI.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace BookRatingAPI.Services
{
    public class BookSyncService : IBookSyncService
    {
        private readonly IMemoryCache _cache;
        private readonly IElasticSearchService _elastic;
        private readonly AppDbContext _context;

        public BookSyncService(IMemoryCache cache, IElasticSearchService elastic, AppDbContext context)
        {
            _cache = cache;
            _elastic = elastic;
            _context = context;
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
            var followerIds = await _context.Follows
                .Where(f => f.FolloweeId == userId)
                .Select(f => f.FollowerId)
                .ToListAsync();

            foreach (var followerId in followerIds)
            {
                _cache.Remove($"recommendations:social:{followerId}");
            }
        }
    }
}
