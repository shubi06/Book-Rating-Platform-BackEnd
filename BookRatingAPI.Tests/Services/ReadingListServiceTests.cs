using BookRatingAPI.Data;
using BookRatingAPI.DTOs;
using BookRatingAPI.Models;
using BookRatingAPI.Models.Enums;
using BookRatingAPI.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace BookRatingAPI.Tests.Services;

public class ReadingListServiceTests
{
    private AppDbContext GetInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    [Fact]
    public async Task AddToReadingListAsync_AddsBook()
    {
        var context = GetInMemoryContext();
        var category = new Category { Id = 1, Name = "Fiction" };
        var book = new Book { Id = 1, Title = "Test Book", Author = "Author", CategoryId = 1, Category = category };
        context.Categories.Add(category);
        context.Books.Add(book);
        await context.SaveChangesAsync();
        var logger = new Mock<ILogger<ReadingListService>>();
        var service = new ReadingListService(context, logger.Object);
        var dto = new AddToReadingListDto { BookId = 1, Status = ReadingStatus.WantToRead };

        var (result, error) = await service.AddToReadingListAsync(1, dto);

        Assert.NotNull(result);
        Assert.Null(error);
        Assert.Equal("Test Book", result.BookTitle);
    }

    [Fact]
    public async Task AddToReadingListAsync_ReturnsError_WhenBookNotFound()
    {
        var context = GetInMemoryContext();
        var logger = new Mock<ILogger<ReadingListService>>();
        var service = new ReadingListService(context, logger.Object);
        var dto = new AddToReadingListDto { BookId = 999, Status = ReadingStatus.WantToRead };

        var (result, error) = await service.AddToReadingListAsync(1, dto);

        Assert.Null(result);
        Assert.NotNull(error);
    }

    [Fact]
    public async Task AddToReadingListAsync_ReturnsError_WhenDuplicate()
    {
        var context = GetInMemoryContext();
        var category = new Category { Id = 1, Name = "Fiction" };
        var book = new Book { Id = 1, Title = "Test Book", Author = "Author", CategoryId = 1, Category = category };
        context.Categories.Add(category);
        context.Books.Add(book);
        context.ReadingLists.Add(new ReadingList { UserId = 1, BookId = 1, Status = ReadingStatus.Read });
        await context.SaveChangesAsync();
        var logger = new Mock<ILogger<ReadingListService>>();
        var service = new ReadingListService(context, logger.Object);
        var dto = new AddToReadingListDto { BookId = 1, Status = ReadingStatus.WantToRead };

        var (result, error) = await service.AddToReadingListAsync(1, dto);

        Assert.Null(result);
        Assert.NotNull(error);
    }

    [Fact]
    public async Task GetReadingListAsync_ReturnsAllEntries()
    {
        var context = GetInMemoryContext();
        var category = new Category { Id = 1, Name = "Fiction" };
        var book = new Book { Id = 1, Title = "Test Book", Author = "Author", CategoryId = 1, Category = category };
        context.Categories.Add(category);
        context.Books.Add(book);
        context.ReadingLists.Add(new ReadingList { UserId = 1, BookId = 1, Status = ReadingStatus.Read, Book = book });
        await context.SaveChangesAsync();
        var logger = new Mock<ILogger<ReadingListService>>();
        var service = new ReadingListService(context, logger.Object);

        var result = await service.GetReadingListAsync(1, null);

        Assert.Single(result);
    }

    [Fact]
    public async Task GetReadingListAsync_FiltersByStatus()
    {
        var context = GetInMemoryContext();
        var category = new Category { Id = 1, Name = "Fiction" };
        var book1 = new Book { Id = 1, Title = "Book1", Author = "Author", CategoryId = 1, Category = category };
        var book2 = new Book { Id = 2, Title = "Book2", Author = "Author", CategoryId = 1, Category = category };
        context.Categories.Add(category);
        context.Books.AddRange(book1, book2);
        context.ReadingLists.AddRange(
            new ReadingList { UserId = 1, BookId = 1, Status = ReadingStatus.WantToRead, Book = book1 },
            new ReadingList { UserId = 1, BookId = 2, Status = ReadingStatus.Read, Book = book2 }
        );
        await context.SaveChangesAsync();
        var logger = new Mock<ILogger<ReadingListService>>();
        var service = new ReadingListService(context, logger.Object);

        var result = await service.GetReadingListAsync(1, ReadingStatus.WantToRead);

        Assert.Single(result);
        Assert.Equal("Book1", result[0].BookTitle);
    }

    [Fact]
    public async Task UpdateStatusAsync_UpdatesStatus()
    {
        var context = GetInMemoryContext();
        var category = new Category { Id = 1, Name = "Fiction" };
        var book = new Book { Id = 1, Title = "Test Book", Author = "Author", CategoryId = 1, Category = category };
        context.Categories.Add(category);
        context.Books.Add(book);
        context.ReadingLists.Add(new ReadingList { Id = 1, UserId = 1, BookId = 1, Status = ReadingStatus.Read, Book = book });
        await context.SaveChangesAsync();
        var logger = new Mock<ILogger<ReadingListService>>();
        var service = new ReadingListService(context, logger.Object);
        var dto = new UpdateReadingListStatusDto { Status = ReadingStatus.Read };

        var (result, error) = await service.UpdateStatusAsync(1, 1, dto);

        Assert.NotNull(result);
        Assert.Null(error);
        Assert.Equal(ReadingStatus.Read, result.Status);
    }

    [Fact]
    public async Task UpdateStatusAsync_ReturnsError_WhenNotFound()
    {
        var context = GetInMemoryContext();
        var logger = new Mock<ILogger<ReadingListService>>();
        var service = new ReadingListService(context, logger.Object);
        var dto = new UpdateReadingListStatusDto { Status = ReadingStatus.Read };

        var (result, error) = await service.UpdateStatusAsync(1, 999, dto);

        Assert.Null(result);
        Assert.NotNull(error);
    }

    [Fact]
    public async Task RemoveFromReadingListAsync_RemovesEntry()
    {
        var context = GetInMemoryContext();
        context.ReadingLists.Add(new ReadingList { Id = 1, UserId = 1, BookId = 1, Status = ReadingStatus.Read });
        await context.SaveChangesAsync();
        var logger = new Mock<ILogger<ReadingListService>>();
        var service = new ReadingListService(context, logger.Object);

        var result = await service.RemoveFromReadingListAsync(1, 1);

        Assert.True(result);
    }

    [Fact]
    public async Task RemoveFromReadingListAsync_ReturnsFalse_WhenNotFound()
    {
        var context = GetInMemoryContext();
        var logger = new Mock<ILogger<ReadingListService>>();
        var service = new ReadingListService(context, logger.Object);

        var result = await service.RemoveFromReadingListAsync(1, 999);

        Assert.False(result);
    }
}