using System.Threading.Tasks;
using BookRatingAPI.DTOs.AuthDTOs;
using BookRatingAPI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace BookRatingAPI.Controllers;

// Handles user authentication (login and registration)
// All endpoints are public (no [Authorize] required)
[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(IAuthService authService, ILogger<AuthController> logger)
    {
        _authService = authService;
        _logger = logger;
    }

    // POST /api/auth/register
    // Registers a new user and returns JWT token
    [HttpPost("register")]
    public async Task<ActionResult<AuthResponseDto>> Register(RegisterDto dto)
    {
        if (!ModelState.IsValid)
        {
            _logger.LogWarning("Registration failed: Invalid model state for email {Email}", dto.Email);
            return BadRequest(ModelState);
        }

        _logger.LogInformation("Registration attempt for email: {Email}", dto.Email);

        var result = await _authService.RegisterAsync(dto);

        if (result == null)
        {
            _logger.LogWarning("Registration failed: Email or username already exists for {Email}", dto.Email);
            return BadRequest(new { Message = "Email or username already exists" });
        }

        _logger.LogInformation("User registered successfully: {Username}", result.User.Username);
        return Ok(result);
    }

    // POST /api/auth/login
    // Authenticates user and returns JWT token
    [HttpPost("login")]
    public async Task<ActionResult<AuthResponseDto>> Login(LoginDto dto)
    {
        if (!ModelState.IsValid)
        {
            _logger.LogWarning("Login failed: Invalid model state");
            return BadRequest(ModelState);
        }

        _logger.LogInformation("Login attempt for email: {Email}", dto.Email);

        var result = await _authService.LoginAsync(dto);

        if (result == null)
        {
            _logger.LogWarning("Login failed: Invalid credentials for email {Email}", dto.Email);
            // Generic error message prevents user enumeration
            return Unauthorized(new { Message = "Invalid credentials" });
        }

        _logger.LogInformation("User logged in successfully: {Username}", result.User.Username);
        return Ok(result);
    }
}
