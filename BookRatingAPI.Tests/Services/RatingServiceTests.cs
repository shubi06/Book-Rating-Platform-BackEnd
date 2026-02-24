using BookRatingAPI.Data;
using BookRatingAPI.DTOs;
using BookRatingAPI.Models;
using BookRatingAPI.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace BookRatingAPI.Tests.Services;

public class RatingServiceTests
{
    private AppDbContext GetInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    [Fact]
    public async Task GetBookRatingsAsync_ReturnsRatingsForBook()
    {
        var context = GetInMemoryContext();
        var user = new User { Id = 1, Username = "user1", Email = "user1@test.com", PasswordHash = "hash" };
        context.Users.Add(user);
        context.Ratings.AddRange(
            new Rating { Id = 1, BookId = 1, UserId = 1, Score = 5, User = user },
            new Rating { Id = 2, BookId = 1, UserId = 1, Score = 4, User = user }
        );
        await context.SaveChangesAsync();
        var logger = new Mock<ILogger<RatingService>>();
        var sync = new Mock<IBookSyncService>();
        var service = new RatingService(context, logger.Object, sync.Object);

        var result = await service.GetBookRatingsAsync(1);

        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task GetUserRatingsAsync_ReturnsRatingsForUser()
    {
        var context = GetInMemoryContext();
        var user = new User { Id = 1, Username = "user1", Email = "user1@test.com", PasswordHash = "hash" };
        var book1 = new Book { Id = 1, Title = "Book1", Author = "Author1" };
        var book2 = new Book { Id = 2, Title = "Book2", Author = "Author2" };
        context.Users.Add(user);
        context.Books.AddRange(book1, book2);
        context.Ratings.AddRange(
            new Rating { Id = 1, BookId = 1, UserId = 1, Score = 5, User = user, Book = book1 },
            new Rating { Id = 2, BookId = 2, UserId = 1, Score = 3, User = user, Book = book2 }
        );
        await context.SaveChangesAsync();
        var logger = new Mock<ILogger<RatingService>>();
        var sync = new Mock<IBookSyncService>();
        var service = new RatingService(context, logger.Object, sync.Object);

        var result = await service.GetUserRatingsAsync(1);

        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task CreateRatingAsync_CreatesNewRating()
    {
        var context = GetInMemoryContext();
        var user = new User { Id = 1, Username = "user1", Email = "user1@test.com", PasswordHash = "hash" };
        context.Users.Add(user);
        await context.SaveChangesAsync();
        var logger = new Mock<ILogger<RatingService>>();
        var sync = new Mock<IBookSyncService>();
        var service = new RatingService(context, logger.Object, sync.Object);
        var dto = new CreateRatingDto { BookId = 1, Score = 5, Comment = "Great!" };

        var result = await service.CreateRatingAsync(1, dto);

        Assert.NotNull(result);
        Assert.Equal(5, result.Score);
    }

    [Fact]
    public async Task CreateRatingAsync_ReturnsNull_WhenUserAlreadyRated()
    {
        var context = GetInMemoryContext();
        var user = new User { Id = 1, Username = "user1", Email = "user1@test.com", PasswordHash = "hash" };
        context.Users.Add(user);
        context.Ratings.Add(new Rating { BookId = 1, UserId = 1, Score = 4, User = user });
        await context.SaveChangesAsync();
        var logger = new Mock<ILogger<RatingService>>();
        var sync = new Mock<IBookSyncService>();
        var service = new RatingService(context, logger.Object, sync.Object);
        var dto = new CreateRatingDto { BookId = 1, Score = 5, Comment = "Great!" };

        var result = await service.CreateRatingAsync(1, dto);

        Assert.Null(result);
    }

    [Fact]
    public async Task UpdateRatingAsync_UpdatesExistingRating()
    {
        var context = GetInMemoryContext();
        var user = new User { Id = 1, Username = "user1", Email = "user1@test.com", PasswordHash = "hash" };
        context.Users.Add(user);
        context.Ratings.Add(new Rating { Id = 1, BookId = 1, UserId = 1, Score = 3, User = user });
        await context.SaveChangesAsync();
        var logger = new Mock<ILogger<RatingService>>();
        var sync = new Mock<IBookSyncService>();
        var service = new RatingService(context, logger.Object, sync.Object);
        var dto = new CreateRatingDto { BookId = 1, Score = 5, Comment = "Updated!" };

        var result = await service.UpdateRatingAsync(1, 1, dto);

        Assert.NotNull(result);
        Assert.Equal(5, result.Score);
    }

    [Fact]
    public async Task UpdateRatingAsync_ReturnsNull_WhenUnauthorized()
    {
        var context = GetInMemoryContext();
        var user = new User { Id = 1, Username = "user1", Email = "user1@test.com", PasswordHash = "hash" };
        context.Users.Add(user);
        context.Ratings.Add(new Rating { Id = 1, BookId = 1, UserId = 1, Score = 3, User = user });
        await context.SaveChangesAsync();
        var logger = new Mock<ILogger<RatingService>>();
        var sync = new Mock<IBookSyncService>();
        var service = new RatingService(context, logger.Object, sync.Object);
        var dto = new CreateRatingDto { BookId = 1, Score = 5 };

        var result = await service.UpdateRatingAsync(1, 999, dto);

        Assert.Null(result);
    }

    [Fact]
    public async Task DeleteRatingAsync_DeletesRating()
    {
        var context = GetInMemoryContext();
        var user = new User { Id = 1, Username = "user1", Email = "user1@test.com", PasswordHash = "hash" };
        context.Users.Add(user);
        context.Ratings.Add(new Rating { Id = 1, BookId = 1, UserId = 1, Score = 3, User = user });
        await context.SaveChangesAsync();
        var logger = new Mock<ILogger<RatingService>>();
        var sync = new Mock<IBookSyncService>();
        var service = new RatingService(context, logger.Object, sync.Object);

        var result = await service.DeleteRatingAsync(1, 1);

        Assert.True(result);
    }

    [Fact]
    public async Task DeleteRatingAsync_ReturnsFalse_WhenUnauthorized()
    {
        var context = GetInMemoryContext();
        var user = new User { Id = 1, Username = "user1", Email = "user1@test.com", PasswordHash = "hash" };
        context.Users.Add(user);
        context.Ratings.Add(new Rating { Id = 1, BookId = 1, UserId = 1, Score = 3, User = user });
        await context.SaveChangesAsync();
        var logger = new Mock<ILogger<RatingService>>();
        var sync = new Mock<IBookSyncService>();
        var service = new RatingService(context, logger.Object, sync.Object);

        var result = await service.DeleteRatingAsync(1, 999);

        Assert.False(result);
    }
}
