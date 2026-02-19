using System.Collections.Generic;
using System.Threading.Tasks;
using BookRatingAPI.Models;

namespace BookRatingAPI.Services;

public interface ICategoryService
{
    Task<List<Category>> GetAllCategoriesAsync();
    Task<Category> CreateCategoryAsync(Category category);
    Task<Category?> UpdateCategoryAsync(int id, Category category);
    Task<bool> DeleteCategoryAsync(int id);
}
