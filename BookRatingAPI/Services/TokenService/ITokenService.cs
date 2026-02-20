using BookRatingAPI.Models;

namespace BookRatingAPI.Services;

public interface ITokenService
{
    string GenerateToken(User user);
}