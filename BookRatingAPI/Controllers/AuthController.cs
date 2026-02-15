using BookRatingAPI.DTOs.AuthDTOs;
using BookRatingAPI.Services;
using Microsoft.AspNetCore.Mvc;

namespace BookRatingAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    
    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }
    
    [HttpPost("register")]
    public async Task<ActionResult<AuthResponseDto>> Register(RegisterDto dto)
    {
        var result = await _authService.RegisterAsync(dto);
        
        if (result == null)
            return BadRequest("Email or username already exists");
        
        return Ok(result);
    }
}