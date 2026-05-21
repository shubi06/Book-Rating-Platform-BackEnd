using BookRatingAPI.DTOs;

namespace BookRatingAPI.Services
{
    public interface IRecommendationsService
    {
        Task<List<BookDto>> GetRecommendationsAsync(int userId);
    }
}
