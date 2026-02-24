using BookRatingAPI.Data;
using BookRatingAPI.DTOs;
using BookRatingAPI.Models;
using BookRatingAPI.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace BookRatingAPI.Tests.Services;

public class CategoryServiceTests
{
    private AppDbContext GetInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    [Fact]
    public async Task GetAllCategoriesAsync_ReturnsAllCategories()
    {
        var context = GetInMemoryContext();
        var logger = new Mock<ILogger<CategoryService>>();
        context.Categories.AddRange(
            new Category { Id = 1, Name = "Fiction" },
            new Category { Id = 2, Name = "Science" }
        );
        await context.SaveChangesAsync();

        var service = new CategoryService(context, logger.Object);
        var result = await service.GetAllCategoriesAsync();

        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task CreateCategoryAsync_CreatesCategory()
    {
        var context = GetInMemoryContext();
        var logger = new Mock<ILogger<CategoryService>>();
        var service = new CategoryService(context, logger.Object);
        var dto = new CreateCategoryDto { Name = "History" };

        var result = await service.CreateCategoryAsync(dto);

        Assert.NotNull(result);
        Assert.Equal("History", result.Name);
    }

    [Fact]
    public async Task UpdateCategoryAsync_UpdatesExistingCategory()
    {
        var context = GetInMemoryContext();
        var logger = new Mock<ILogger<CategoryService>>();
        context.Categories.Add(new Category { Id = 1, Name = "Old Name" });
        await context.SaveChangesAsync();

        var service = new CategoryService(context, logger.Object);
        var dto = new CreateCategoryDto { Name = "New Name" };
        var result = await service.UpdateCategoryAsync(1, dto);

        Assert.NotNull(result);
        Assert.Equal("New Name", result.Name);
    }

    [Fact]
    public async Task UpdateCategoryAsync_ReturnsNull_WhenNotFound()
    {
        var context = GetInMemoryContext();
        var logger = new Mock<ILogger<CategoryService>>();
        var service = new CategoryService(context, logger.Object);
        var dto = new CreateCategoryDto { Name = "Test" };

        var result = await service.UpdateCategoryAsync(999, dto);

        Assert.Null(result);
    }

    [Fact]
    public async Task DeleteCategoryAsync_DeletesCategory()
    {
        var context = GetInMemoryContext();
        var logger = new Mock<ILogger<CategoryService>>();
        context.Categories.Add(new Category { Id = 1, Name = "ToDelete" });
        await context.SaveChangesAsync();

        var service = new CategoryService(context, logger.Object);
        var result = await service.DeleteCategoryAsync(1);

        Assert.True(result);
    }

    [Fact]
    public async Task DeleteCategoryAsync_ReturnsFalse_WhenNotFound()
    {
        var context = GetInMemoryContext();
        var logger = new Mock<ILogger<CategoryService>>();
        var service = new CategoryService(context, logger.Object);

        var result = await service.DeleteCategoryAsync(999);

        Assert.False(result);
    }

    [Fact]
    public async Task DeleteCategoryAsync_ReturnsFalse_WhenCategoryHasBooks()
    {
        var context = GetInMemoryContext();
        var logger = new Mock<ILogger<CategoryService>>();
        var category = new Category { Id = 1, Name = "Fiction" };
        context.Categories.Add(category);
        context.Books.Add(new Book { Id = 1, Title = "Test Book", CategoryId = 1 });
        await context.SaveChangesAsync();

        var service = new CategoryService(context, logger.Object);
        var result = await service.DeleteCategoryAsync(1);

        Assert.False(result);
    }
}
