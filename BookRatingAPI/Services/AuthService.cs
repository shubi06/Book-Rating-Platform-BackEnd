using BookRatingAPI.Data;
using BookRatingAPI.DTOs.AuthDTOs;
using BookRatingAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace BookRatingAPI.Services;

public class AuthService : IAuthService
{
    private readonly AppDbContext _context;
    private readonly ITokenService _tokenService;
    
    public AuthService(AppDbContext context, ITokenService tokenService)
    {
        _context = context;
        _tokenService = tokenService;
    }
    
    public async  Task<AuthResponseDto?> RegisterAsync (RegisterDto dto)
    {
        if (await _context.Users.AnyAsync(u => u.Email == dto.Email))
            return null;
            
        if (await _context.Users.AnyAsync(u => u.Username == dto.Username))
            return null;
        
        var user = new User
        {
            Username = dto.Username,
            Email = dto.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password)
        };
        
        _context.Users.Add(user);
        await _context.SaveChangesAsync();
        
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