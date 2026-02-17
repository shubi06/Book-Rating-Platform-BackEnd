using System.Threading.Tasks;
using BookRatingAPI.DTOs.AuthDTOs;

namespace BookRatingAPI.Services;

public interface IAuthService
{
    Task<AuthResponseDto?> RegisterAsync(RegisterDto dto);
    Task<AuthResponseDto?> LoginAsync(LoginDto dto);
}