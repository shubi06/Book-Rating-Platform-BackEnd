using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BookRatingAPI.Data;
using BookRatingAPI.DTOs;
using BookRatingAPI.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BookRatingAPI.Services;

public class CategoryService : ICategoryService
{
    private readonly AppDbContext _context;
    private readonly ILogger<CategoryService> _logger;

    public CategoryService(AppDbContext context, ILogger<CategoryService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<List<CategoryDto>> GetAllCategoriesAsync()
    {
        _logger.LogInformation("Fetching all categories");
        var categories = await _context.Categories
            .Select(c => new CategoryDto { Id = c.Id, Name = c.Name })
            .ToListAsync();
        _logger.LogInformation("Retrieved {Count} categories", categories.Count);
        return categories;
    }

    public async Task<CategoryDto> CreateCategoryAsync(CreateCategoryDto dto)
    {
        _logger.LogInformation("Creating category: Name={Name}", dto.Name);
        var category = new Category { Name = dto.Name };
        
        _context.Categories.Add(category);
        await _context.SaveChangesAsync();
        
        _logger.LogInformation("Category created successfully: Id={CategoryId}", category.Id);
        return new CategoryDto { Id = category.Id, Name = category.Name };
    }

    public async Task<CategoryDto?> UpdateCategoryAsync(int id, CreateCategoryDto dto)
    {
        _logger.LogInformation("Updating category: Id={CategoryId}", id);
        var existing = await _context.Categories.FindAsync(id);

        if (existing == null)
        {
            _logger.LogWarning("Category not found: Id={CategoryId}", id);
            return null;
        }

        existing.Name = dto.Name;
        await _context.SaveChangesAsync();

        _logger.LogInformation("Category updated successfully: Id={CategoryId}", id);
        return new CategoryDto { Id = existing.Id, Name = existing.Name };
    }

    public async Task<bool> DeleteCategoryAsync(int id)
    {
        _logger.LogInformation("Deleting category: Id={CategoryId}", id);
        var category = await _context.Categories.FindAsync(id);

        if (category == null)
        {
            _logger.LogWarning("Category not found: Id={CategoryId}", id);
            return false;
        }

        var hasBooks = await _context.Books.AnyAsync(b => b.CategoryId == id);
        if (hasBooks)
        {
            _logger.LogWarning("Cannot delete category with books: Id={CategoryId}", id);
            return false;
        }

        _context.Categories.Remove(category);
        await _context.SaveChangesAsync();
        
        _logger.LogInformation("Category deleted successfully: Id={CategoryId}", id);
        return true;
    }
}
