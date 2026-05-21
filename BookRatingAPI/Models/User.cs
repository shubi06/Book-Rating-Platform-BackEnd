using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace BookRatingAPI.Models;

// User entity for authentication and authorization
// Passwords are hashed with BCrypt before storage
public class User
{
    public int Id { get; set; }
    
    [Required]
    [MaxLength(100)]
    public string Username { get; set; } = string.Empty;
    
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;
    
    // BCrypt hash of user password (never store plaintext passwords)
    // Format: $2a$10$[22 chars salt][31 chars hash]
    [Required]
    public string PasswordHash { get; set; } = string.Empty;
    
    // Determines if user has admin privileges
    // Used with [Authorize(Roles = "Admin")] for role-based access control
    public bool IsAdmin { get; set; } = false;
    
    // Account creation timestamp (UTC)
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties for EF Core relationships
    public ICollection<Rating> Ratings { get; set; } = new List<Rating>();
    public ICollection<ReadingList> ReadingLists { get; set; } = new List<ReadingList>();

    // Users this user follows (this user is the Follower in the join row)
    public ICollection<Follow> Following { get; set; } = new List<Follow>();
    // Users following this user (this user is the Followee in the join row)
    public ICollection<Follow> Followers { get; set; } = new List<Follow>();
}
