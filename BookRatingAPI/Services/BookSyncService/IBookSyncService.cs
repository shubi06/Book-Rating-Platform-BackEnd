namespace BookRatingAPI.Services
{
    public interface IBookSyncService
    {
        Task SyncBookAsync(int bookId);
        Task RemoveBookAsync(int bookId);
        void InvalidateUserRecommendations(int userId);
        void InvalidateSimilarReaders(int userId);
    }
}
