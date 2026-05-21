using BookRatingAPI.DTOs;

namespace BookRatingAPI.Services
{
    public interface IRecommendationsService
    {
        Task<List<BookDto>> GetRecommendationsAsync(int userId);
        Task<List<BookDto>> GetSocialRecommendationsAsync(int userId);
    }
}
