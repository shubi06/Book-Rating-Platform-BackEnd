using Microsoft.Extensions.Caching.Memory;

namespace BookRatingAPI.Services
{
    public class BookSyncService : IBookSyncService
    {
        private readonly IMemoryCache _cache;
        private readonly IElasticSearchService _elastic;

        public BookSyncService(IMemoryCache cache, IElasticSearchService elastic)
        {
            _cache = cache;
            _elastic = elastic;
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

        // Broad-key removal across the legal 1..50 limit range. The endpoint validates
        // the limit query parameter to this range, so any cached entry for this user
        // lives under one of these keys. 50 in-memory removals on a rating mutation
        // is cheaper than recomputing Jaccard sets on the next read.
        public void InvalidateSimilarReaders(int userId)
        {
            for (int n = 1; n <= 50; n++)
            {
                _cache.Remove($"similar-readers:{userId}:limit:{n}");
            }
        }
    }
}
