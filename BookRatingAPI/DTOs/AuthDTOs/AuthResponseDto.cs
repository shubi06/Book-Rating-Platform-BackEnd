namespace BookRatingAPI.DTOs.AuthDTOs;

public class AuthResponseDto
{
    public string Token { get; set; } = string.Empty;
    public UserDto User { get; set; } = null!;
}