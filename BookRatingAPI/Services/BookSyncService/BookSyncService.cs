using Microsoft.Extensions.Caching.Memory;

namespace BookRatingAPI.Services
{
    public class BookSyncService : IBookSyncService
    {
        private readonly IMemoryCache _cache;
        private readonly IElasticSearchService _elastic;
        private readonly ILogger<BookSyncService> _logger;

        public BookSyncService(IMemoryCache cache, IElasticSearchService elastic, ILogger<BookSyncService> logger)
        {
            _cache = cache;
            _elastic = elastic;
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

        // Global version-counter invalidation. A per-user broad-key sweep only catches
        // entries where userId is the target; it misses entries where userId is a candidate
        // in some other target's cached neighbor list. Bumping a single version segment
        // baked into every similar-readers:* key invalidates the whole similarity cache
        // in O(1) and is correct for both target-side and candidate-side mutations.
        public void InvalidateSimilarReaders(int userId)
        {
            const string versionKey = "similar-readers:version";
            var current = _cache.Get<long?>(versionKey) ?? 0L;
            var next = current + 1L;
            _cache.Set(versionKey, next);
            _logger.LogDebug(
                "Similar-readers cache version bumped to {Version} after rating mutation by UserId={UserId}",
                next, userId);
        }
    }
}
