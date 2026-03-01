using BookRatingAPI.Models;

namespace BookRatingAPI.Services;

public interface ITokenService
{
    // Generates a JWT token for the specified user
    // Returns JWT token string (format: header.payload.signature)
    string GenerateToken(User user);
}
