using System.Collections.Generic;
using System.Threading.Tasks;
using BookRatingAPI.DTOs;
using BookRatingAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace BookRatingAPI.Controllers;

/// <summary>
/// Controller for managing book categories (Admin only)
/// </summary>
[Authorize(Roles = "Admin")]
[ApiController]
[Route("api/[controller]")]
public class CategoriesController : ControllerBase
{
    private readonly ICategoryService _categoryService;
    private readonly ILogger<CategoriesController> _logger;

    public CategoriesController(ICategoryService categoryService, ILogger<CategoriesController> logger)
    {
        _categoryService = categoryService;
        _logger = logger;
    }

    /// <summary>
    /// Get all categories (Public access)
    /// </summary>
    /// <returns>List of all categories</returns>
    [AllowAnonymous]
    [HttpGet]
    public async Task<ActionResult<IEnumerable<CategoryDto>>> GetCategories()
    {
        _logger.LogInformation("Fetching all categories");
        var categories = await _categoryService.GetAllCategoriesAsync();
        _logger.LogInformation("Retrieved {Count} categories", categories.Count);
        return Ok(categories);
    }

    /// <summary>
    /// Create a new category
    /// </summary>
    /// <param name="dto">Category data</param>
    /// <returns>The created category</returns>
    [HttpPost]
    public async Task<ActionResult<CategoryDto>> CreateCategory(CreateCategoryDto dto)
    {
        if (!ModelState.IsValid)
        {
            _logger.LogWarning("Invalid model state for category creation");
            return BadRequest(ModelState);
        }

        _logger.LogInformation("Creating category: Name={Name}", dto.Name);
        var created = await _categoryService.CreateCategoryAsync(dto);
        _logger.LogInformation("Category created successfully: Id={CategoryId}", created.Id);
        return CreatedAtAction(nameof(GetCategories), new { id = created.Id }, created);
    }

    /// <summary>
    /// Update an existing category
    /// </summary>
    /// <param name="id">The ID of the category to update</param>
    /// <param name="dto">Updated category data</param>
    /// <returns>The updated category</returns>
    [HttpPut("{id}")]
    public async Task<ActionResult<CategoryDto>> UpdateCategory(int id, CreateCategoryDto dto)
    {
        if (!ModelState.IsValid)
        {
            _logger.LogWarning("Invalid model state for category update: Id={CategoryId}", id);
            return BadRequest(ModelState);
        }

        _logger.LogInformation("Updating category: Id={CategoryId}", id);
        var updated = await _categoryService.UpdateCategoryAsync(id, dto);

        if (updated == null)
        {
            _logger.LogWarning("Category not found: Id={CategoryId}", id);
            return NotFound(new { Message = "Category not found" });
        }

        _logger.LogInformation("Category updated successfully: Id={CategoryId}", id);
        return Ok(updated);
    }

    /// <summary>
    /// Delete a category
    /// </summary>
    /// <param name="id">The ID of the category to delete</param>
    /// <returns>No content on success</returns>
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteCategory(int id)
    {
        _logger.LogInformation("Deleting category: Id={CategoryId}", id);
        var deleted = await _categoryService.DeleteCategoryAsync(id);

        if (!deleted)
        {
            _logger.LogWarning("Category not found or has books: Id={CategoryId}", id);
            return BadRequest(new { Message = "Category not found or has books" });
        }

        _logger.LogInformation("Category deleted successfully: Id={CategoryId}", id);
        return NoContent();
    }
}
