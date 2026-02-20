using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BookRatingAPI.Data;
using BookRatingAPI.DTOs;
using BookRatingAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace BookRatingAPI.Services;

/// <summary>
/// Service for managing book categories
/// </summary>
public class CategoryService : ICategoryService
{
    private readonly AppDbContext _context;

    public CategoryService(AppDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Get all categories
    /// </summary>
    /// <returns>List of all categories</returns>
    public async Task<List<CategoryDto>> GetAllCategoriesAsync()
    {
        return await _context.Categories
            .Select(c => new CategoryDto { Id = c.Id, Name = c.Name })
            .ToListAsync();
    }

    /// <summary>
    /// Create a new category
    /// </summary>
    /// <param name="dto">Category creation data</param>
    /// <returns>The created category</returns>
    public async Task<CategoryDto> CreateCategoryAsync(CreateCategoryDto dto)
    {
        var category = new Category { Name = dto.Name };
        
        _context.Categories.Add(category);
        await _context.SaveChangesAsync();
        
        return new CategoryDto { Id = category.Id, Name = category.Name };
    }

    /// <summary>
    /// Update an existing category
    /// </summary>
    /// <param name="id">The ID of the category to update</param>
    /// <param name="dto">Updated category data</param>
    /// <returns>The updated category, or null if not found</returns>
    public async Task<CategoryDto?> UpdateCategoryAsync(int id, CreateCategoryDto dto)
    {
        var existing = await _context.Categories.FindAsync(id);

        if (existing == null)
            return null;

        existing.Name = dto.Name;
        await _context.SaveChangesAsync();

        return new CategoryDto { Id = existing.Id, Name = existing.Name };
    }

    /// <summary>
    /// Delete a category
    /// </summary>
    /// <param name="id">The ID of the category to delete</param>
    /// <returns>True if deleted successfully, false if not found or has books</returns>
    public async Task<bool> DeleteCategoryAsync(int id)
    {
        var category = await _context.Categories.FindAsync(id);

        if (category == null)
            return false;

        // Check if category has books
        var hasBooks = await _context.Books.AnyAsync(b => b.CategoryId == id);
        if (hasBooks)
            return false;

        _context.Categories.Remove(category);
        await _context.SaveChangesAsync();
        
        return true;
    }
}
