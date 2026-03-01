using System.Threading.Tasks;
using BookRatingAPI.Data;
using BookRatingAPI.DTOs.AuthDTOs;
using BookRatingAPI.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BookRatingAPI.Services;

// Service for user authentication (login and registration)
// Uses BCrypt for password hashing and JWT for session management
public class AuthService : IAuthService
{
    private readonly AppDbContext _context;
    private readonly ITokenService _tokenService;
    private readonly ILogger<AuthService> _logger;

    public AuthService(AppDbContext context, ITokenService tokenService, ILogger<AuthService> logger)
    {
        _context = context;
        _tokenService = tokenService;
        _logger = logger;
    }

    // Registers a new user with hashed password
    // Returns null if email or username already exists
    public async Task<AuthResponseDto?> RegisterAsync(RegisterDto dto)
    {
        _logger.LogInformation("Starting user registration for email: {Email}", dto.Email);

        // Check for duplicate email
        if (await _context.Users.AnyAsync(u => u.Email == dto.Email))
        {
            _logger.LogWarning("Registration failed: Email already exists - {Email}", dto.Email);
            return null;
        }

        // Check for duplicate username
        if (await _context.Users.AnyAsync(u => u.Username == dto.Username))
        {
            _logger.LogWarning("Registration failed: Username already exists - {Username}", dto.Username);
            return null;
        }

        // Create user with hashed password (BCrypt with cost factor 10)
        var user = new User
        {
            Username = dto.Username,
            Email = dto.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password)
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        _logger.LogInformation("User created successfully: UserId={UserId}, Username={Username}", user.Id, user.Username);

        // Generate JWT token for automatic login after registration
        var token = _tokenService.GenerateToken(user);

        return new AuthResponseDto
        {
            Token = token,
            User = new UserDto
            {
                Id = user.Id,
                Username = user.Username,
                Email = user.Email,
                IsAdmin = user.IsAdmin
            }
        };
    }

    // Authenticates user with email and password
    // Returns null if credentials are invalid (prevents user enumeration)
    public async Task<AuthResponseDto?> LoginAsync(LoginDto dto)
    {
        _logger.LogInformation("Login attempt for email: {Email}", dto.Email);

        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == dto.Email);

        if (user == null)
        {
            _logger.LogWarning("Login failed: User not found for email {Email}", dto.Email);
            return null;  // Don't reveal whether email exists
        }

        // Verify password using BCrypt (constant-time comparison prevents timing attacks)
        if (!BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
        {
            _logger.LogWarning("Login failed: Invalid password for email {Email}", dto.Email);
            return null;
        }

        _logger.LogInformation("User authenticated successfully: UserId={UserId}, Username={Username}", user.Id, user.Username);

        var token = _tokenService.GenerateToken(user);

        return new AuthResponseDto
        {
            Token = token,
            User = new UserDto
            {
                Id = user.Id,
                Username = user.Username,
                Email = user.Email,
                IsAdmin = user.IsAdmin
            }
        };
    }
}
