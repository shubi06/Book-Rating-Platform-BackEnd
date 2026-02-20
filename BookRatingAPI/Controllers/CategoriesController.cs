using System.Collections.Generic;
using System.Threading.Tasks;
using BookRatingAPI.Models;
using BookRatingAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

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

    public CategoriesController(ICategoryService categoryService)
    {
        _categoryService = categoryService;
    }

    /// <summary>
    /// Get all categories (Public access)
    /// </summary>
    /// <returns>List of all categories</returns>
    [AllowAnonymous]
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Category>>> GetCategories()
    {
        var categories = await _categoryService.GetAllCategoriesAsync();
        return Ok(categories);
    }

    /// <summary>
    /// Create a new category
    /// </summary>
    /// <param name="category">Category data</param>
    /// <returns>The created category</returns>
    [HttpPost]
    public async Task<ActionResult<Category>> CreateCategory(Category category)
    {
        var created = await _categoryService.CreateCategoryAsync(category);
        return CreatedAtAction(nameof(GetCategories), new { id = created.Id }, created);
    }

    /// <summary>
    /// Update an existing category
    /// </summary>
    /// <param name="id">The ID of the category to update</param>
    /// <param name="category">Updated category data</param>
    /// <returns>The updated category</returns>
    [HttpPut("{id}")]
    public async Task<ActionResult<Category>> UpdateCategory(int id, Category category)
    {
        var updated = await _categoryService.UpdateCategoryAsync(id, category);

        if (updated == null)
            return NotFound();

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
        var deleted = await _categoryService.DeleteCategoryAsync(id);

        if (!deleted)
            return BadRequest("Category not found or has books");

        return NoContent();
    }
}
