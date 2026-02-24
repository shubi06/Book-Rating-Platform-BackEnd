using BookRatingAPI.Data;
using BookRatingAPI.DTOs;
using BookRatingAPI.Models;
using BookRatingAPI.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace BookRatingAPI.Tests.Services;

public class BookServiceTests
{
    private AppDbContext GetInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    [Fact]
    public async Task GetBooksAsync_ReturnsAllBooks()
    {
        var context = GetInMemoryContext();
        var category = new Category { Id = 1, Name = "Fiction" };
        context.Categories.Add(category);
        context.Books.Add(
            new Book
            {
                Id = 1,
                Title = "Book1",
                Author = "Author1",
                CategoryId = 1,
                Category = category,
            }
        );
        await context.SaveChangesAsync();
        var cache = new MemoryCache(new MemoryCacheOptions());
        var logger = new Mock<ILogger<BookService>>();
        var sync = new Mock<IBookSyncService>();
        var service = new BookService(context, cache, logger.Object, sync.Object);

        var result = await service.GetBooksAsync(null, null);

        Assert.Single(result);
    }

    [Fact]
    public async Task GetBookByIdAsync_ReturnsBook()
    {
        var context = GetInMemoryContext();
        var category = new Category { Id = 1, Name = "Fiction" };
        context.Categories.Add(category);
        context.Books.Add(
            new Book
            {
                Id = 1,
                Title = "Test Book",
                Author = "Author",
                CategoryId = 1,
                Category = category,
            }
        );
        await context.SaveChangesAsync();
        var cache = new MemoryCache(new MemoryCacheOptions());
        var logger = new Mock<ILogger<BookService>>();
        var sync = new Mock<IBookSyncService>();
        var service = new BookService(context, cache, logger.Object, sync.Object);

        var result = await service.GetBookByIdAsync(1);

        Assert.NotNull(result);
        Assert.Equal("Test Book", result.Title);
    }

    [Fact]
    public async Task GetBookByIdAsync_ReturnsNull_WhenNotFound()
    {
        var context = GetInMemoryContext();
        var cache = new MemoryCache(new MemoryCacheOptions());
        var logger = new Mock<ILogger<BookService>>();
        var sync = new Mock<IBookSyncService>();
        var service = new BookService(context, cache, logger.Object, sync.Object);

        var result = await service.GetBookByIdAsync(999);

        Assert.Null(result);
    }

    [Fact]
    public async Task CreateBookAsync_CreatesBook()
    {
        var context = GetInMemoryContext();
        var category = new Category { Id = 1, Name = "Fiction" };
        context.Categories.Add(category);
        await context.SaveChangesAsync();
        var cache = new MemoryCache(new MemoryCacheOptions());
        var logger = new Mock<ILogger<BookService>>();
        var sync = new Mock<IBookSyncService>();
        var service = new BookService(context, cache, logger.Object, sync.Object);
        var dto = new CreateBookDto
        {
            Title = "New Book",
            Author = "Author",
            CategoryId = 1,
        };

        var result = await service.CreateBookAsync(dto);

        Assert.NotNull(result);
        Assert.Equal("New Book", result.Title);
    }

    [Fact]
    public async Task UpdateBookAsync_UpdatesBook()
    {
        var context = GetInMemoryContext();
        var category = new Category { Id = 1, Name = "Fiction" };
        context.Categories.Add(category);
        context.Books.Add(
            new Book
            {
                Id = 1,
                Title = "Old Title",
                Author = "Author",
                CategoryId = 1,
                Category = category,
            }
        );
        await context.SaveChangesAsync();
        var cache = new MemoryCache(new MemoryCacheOptions());
        var logger = new Mock<ILogger<BookService>>();
        var sync = new Mock<IBookSyncService>();
        var service = new BookService(context, cache, logger.Object, sync.Object);
        var dto = new CreateBookDto
        {
            Title = "New Title",
            Author = "Author",
            CategoryId = 1,
        };

        var result = await service.UpdateBookAsync(1, dto);

        Assert.NotNull(result);
        Assert.Equal("New Title", result.Title);
    }

    [Fact]
    public async Task UpdateBookAsync_ReturnsNull_WhenNotFound()
    {
        var context = GetInMemoryContext();
        var cache = new MemoryCache(new MemoryCacheOptions());
        var logger = new Mock<ILogger<BookService>>();
        var sync = new Mock<IBookSyncService>();
        var service = new BookService(context, cache, logger.Object, sync.Object);
        var dto = new CreateBookDto
        {
            Title = "Test",
            Author = "Author",
            CategoryId = 1,
        };

        var result = await service.UpdateBookAsync(999, dto);

        Assert.Null(result);
    }

    [Fact]
    public async Task DeleteBookAsync_DeletesBook()
    {
        var context = GetInMemoryContext();
        context.Books.Add(
            new Book
            {
                Id = 1,
                Title = "To Delete",
                Author = "Author",
                CategoryId = 1,
            }
        );
        await context.SaveChangesAsync();
        var cache = new MemoryCache(new MemoryCacheOptions());
        var logger = new Mock<ILogger<BookService>>();
        var sync = new Mock<IBookSyncService>();
        var service = new BookService(context, cache, logger.Object, sync.Object);

        var result = await service.DeleteBookAsync(1);

        Assert.True(result);
    }

    [Fact]
    public async Task DeleteBookAsync_ReturnsFalse_WhenNotFound()
    {
        var context = GetInMemoryContext();
        var cache = new MemoryCache(new MemoryCacheOptions());
        var logger = new Mock<ILogger<BookService>>();
        var sync = new Mock<IBookSyncService>();
        var service = new BookService(context, cache, logger.Object, sync.Object);

        var result = await service.DeleteBookAsync(999);

        Assert.False(result);
    }
}
