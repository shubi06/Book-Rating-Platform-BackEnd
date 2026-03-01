namespace BookRatingAPI.DTOs.AuthDTOs;

// Response DTO containing JWT token and user data
// Returned after successful login or registration
public class AuthResponseDto
{
    // JWT token to be sent in Authorization header: "Bearer {token}"
    public string Token { get; set; } = string.Empty;
    
    // User information (excludes sensitive data like PasswordHash)
    public UserDto User { get; set; } = null!;
}
