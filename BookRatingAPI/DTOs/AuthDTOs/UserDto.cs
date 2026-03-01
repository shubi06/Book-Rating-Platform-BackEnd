namespace BookRatingAPI.DTOs.AuthDTOs;

// DTO for user data (excludes sensitive information like PasswordHash)
public class UserDto
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    
    // Indicates if user has admin privileges
    // Used for UI control and backend authorization with [Authorize(Roles = "Admin")]
    public bool IsAdmin { get; set; }
}
