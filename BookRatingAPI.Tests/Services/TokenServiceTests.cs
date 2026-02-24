using BookRatingAPI.Services;
using BookRatingAPI.Models;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace BookRatingAPI.Tests.Services;

public class TokenServiceTests
{
    private readonly IConfiguration _config;

    public TokenServiceTests()
    {
        var inMemorySettings = new Dictionary<string, string>
        {
            {"Jwt:Key", "ThisIsASecretKeyForTestingPurposesOnly12345"},
            {"Jwt:Issuer", "TestIssuer"},
            {"Jwt:Audience", "TestAudience"}
        };
        _config = new ConfigurationBuilder()
            .AddInMemoryCollection(inMemorySettings!)
            .Build();
    }

    [Fact]
    public void GenerateToken_ReturnsValidToken()
    {
        var service = new TokenService(_config);
        var user = new User { Id = 1, Email = "test@test.com", Username = "testuser", IsAdmin = false };

        var token = service.GenerateToken(user);

        Assert.NotNull(token);
        Assert.NotEmpty(token);
    }

    [Fact]
    public void GenerateToken_ForAdminUser_ReturnsToken()
    {
        var service = new TokenService(_config);
        var user = new User { Id = 2, Email = "admin@test.com", Username = "admin", IsAdmin = true };

        var token = service.GenerateToken(user);

        Assert.NotNull(token);
        Assert.NotEmpty(token);
    }
}
