using BookRatingAPI.Data;
using BookRatingAPI.DTOs.AuthDTOs;
using BookRatingAPI.Models;
using BookRatingAPI.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace BookRatingAPI.Tests.Services;

public class AuthServiceTests
{
    private AppDbContext GetInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    [Fact]
    public async Task RegisterAsync_CreatesNewUser()
    {
        var context = GetInMemoryContext();
        var tokenService = new Mock<ITokenService>();
        tokenService.Setup(x => x.GenerateToken(It.IsAny<User>())).Returns("test-token");
        var logger = new Mock<ILogger<AuthService>>();
        var service = new AuthService(context, tokenService.Object, logger.Object);
        var dto = new RegisterDto { Username = "newuser", Email = "new@test.com", Password = "password123" };

        var result = await service.RegisterAsync(dto);

        Assert.NotNull(result);
        Assert.Equal("test-token", result.Token);
        Assert.Equal("newuser", result.User.Username);
    }

    [Fact]
    public async Task RegisterAsync_ReturnsNull_WhenEmailExists()
    {
        var context = GetInMemoryContext();
        context.Users.Add(new User { Email = "existing@test.com", Username = "existing", PasswordHash = "hash" });
        await context.SaveChangesAsync();
        var tokenService = new Mock<ITokenService>();
        var logger = new Mock<ILogger<AuthService>>();
        var service = new AuthService(context, tokenService.Object, logger.Object);
        var dto = new RegisterDto { Username = "newuser", Email = "existing@test.com", Password = "password123" };

        var result = await service.RegisterAsync(dto);

        Assert.Null(result);
    }

    [Fact]
    public async Task RegisterAsync_ReturnsNull_WhenUsernameExists()
    {
        var context = GetInMemoryContext();
        context.Users.Add(new User { Email = "test@test.com", Username = "existing", PasswordHash = "hash" });
        await context.SaveChangesAsync();
        var tokenService = new Mock<ITokenService>();
        var logger = new Mock<ILogger<AuthService>>();
        var service = new AuthService(context, tokenService.Object, logger.Object);
        var dto = new RegisterDto { Username = "existing", Email = "new@test.com", Password = "password123" };

        var result = await service.RegisterAsync(dto);

        Assert.Null(result);
    }

    [Fact]
    public async Task LoginAsync_ReturnsToken_WhenCredentialsValid()
    {
        var context = GetInMemoryContext();
        var passwordHash = BCrypt.Net.BCrypt.HashPassword("password123");
        context.Users.Add(new User { Id = 1, Email = "user@test.com", Username = "user", PasswordHash = passwordHash });
        await context.SaveChangesAsync();
        var tokenService = new Mock<ITokenService>();
        tokenService.Setup(x => x.GenerateToken(It.IsAny<User>())).Returns("login-token");
        var logger = new Mock<ILogger<AuthService>>();
        var service = new AuthService(context, tokenService.Object, logger.Object);
        var dto = new LoginDto { Email = "user@test.com", Password = "password123" };

        var result = await service.LoginAsync(dto);

        Assert.NotNull(result);
        Assert.Equal("login-token", result.Token);
    }

    [Fact]
    public async Task LoginAsync_ReturnsNull_WhenUserNotFound()
    {
        var context = GetInMemoryContext();
        var tokenService = new Mock<ITokenService>();
        var logger = new Mock<ILogger<AuthService>>();
        var service = new AuthService(context, tokenService.Object, logger.Object);
        var dto = new LoginDto { Email = "notfound@test.com", Password = "password123" };

        var result = await service.LoginAsync(dto);

        Assert.Null(result);
    }

    [Fact]
    public async Task LoginAsync_ReturnsNull_WhenPasswordInvalid()
    {
        var context = GetInMemoryContext();
        var passwordHash = BCrypt.Net.BCrypt.HashPassword("correctpassword");
        context.Users.Add(new User { Email = "user@test.com", Username = "user", PasswordHash = passwordHash });
        await context.SaveChangesAsync();
        var tokenService = new Mock<ITokenService>();
        var logger = new Mock<ILogger<AuthService>>();
        var service = new AuthService(context, tokenService.Object, logger.Object);
        var dto = new LoginDto { Email = "user@test.com", Password = "wrongpassword" };

        var result = await service.LoginAsync(dto);

        Assert.Null(result);
    }
}
