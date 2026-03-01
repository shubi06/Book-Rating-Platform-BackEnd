using System.Threading.Tasks;
using BookRatingAPI.DTOs.AuthDTOs;

namespace BookRatingAPI.Services;

public interface IAuthService
{
    // Registers a new user and returns JWT token
    // Returns null if email or username already exists
    Task<AuthResponseDto?> RegisterAsync(RegisterDto dto);
    
    // Authenticates user and returns JWT token
    // Returns null if credentials are invalid
    Task<AuthResponseDto?> LoginAsync(LoginDto dto);
}
