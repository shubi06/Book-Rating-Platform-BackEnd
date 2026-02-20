using System.Threading.Tasks;
using BookRatingAPI.DTOs.AuthDTOs;
using BookRatingAPI.Services;
using Microsoft.AspNetCore.Mvc;

namespace BookRatingAPI.Controllers;

/// <summary>
/// Controller for user authentication
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    /// <summary>
    /// Register a new user
    /// </summary>
    /// <param name="dto">Registration data</param>
    /// <returns>Authentication response with token and user data</returns>
    [HttpPost("register")]
    public async Task<ActionResult<AuthResponseDto>> Register(RegisterDto dto)
    {
        var result = await _authService.RegisterAsync(dto);

        if (result == null)
            return BadRequest("Email or username already exists");

        return Ok(result);
    }

    /// <summary>
    /// Authenticate a user
    /// </summary>
    /// <param name="dto">Login credentials</param>
    /// <returns>Authentication response with token and user data</returns>
    [HttpPost("login")]
    public async Task<ActionResult<AuthResponseDto>> Login(LoginDto dto)
    {
        var result = await _authService.LoginAsync(dto);

        if (result == null)
            return Unauthorized("Invalid credentials");

        return Ok(result);
    }
}