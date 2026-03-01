using System.ComponentModel.DataAnnotations;

namespace BookRatingAPI.DTOs.AuthDTOs;

public class LoginDto
{
    [Required]
    public string Email { get; set; } = string.Empty;
    
    // Password sent in plaintext - HTTPS required for security
    [Required]
    public string Password { get; set; } = string.Empty;
}
