using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using BookRatingAPI.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace BookRatingAPI.Services;

// Service for generating JWT tokens for authenticated users
public class TokenService : ITokenService
{
    private readonly IConfiguration _config;

    public TokenService(IConfiguration config)
    {
        _config = config;
    }

    // Generates a JWT token containing user claims
    // Token expires after 7 days and is signed with HMAC-SHA256
    public string GenerateToken(User user)
    {
        // Create claims (user information embedded in token)
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Name, user.Username),
            new Claim(ClaimTypes.Role, user.IsAdmin ? "Admin" : "User")
        };
    
        // Create signing key from secret (must be at least 256 bits for HS256)
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]!));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
    
        // Build the token
        var token = new JwtSecurityToken(
            issuer: _config["Jwt:Issuer"],
            audience: _config["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddDays(7),  // TODO: Implement refresh tokens for longer sessions
            signingCredentials: creds
        );
    
        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
