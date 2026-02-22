using System.Collections.Generic;
using System.Threading.Tasks;
using BookRatingAPI.DTOs;

namespace BookRatingAPI.Services;

public interface IRatingService
{
    Task<List<RatingDto>> GetBookRatingsAsync(int bookId);
    Task<List<RatingDto>> GetUserRatingsAsync(int userId);
    Task<RatingDto?> CreateRatingAsync(int userId, CreateRatingDto dto);
    Task<RatingDto?> UpdateRatingAsync(int id, int userId, CreateRatingDto dto);
    Task<bool> DeleteRatingAsync(int id, int userId);
}
