using System.Collections.Generic;
using System.Threading.Tasks;
using BookRatingAPI.DTOs;

namespace BookRatingAPI.Services;

public interface ICategoryService
{
    Task<List<CategoryDto>> GetAllCategoriesAsync();
    Task<CategoryDto> CreateCategoryAsync(CreateCategoryDto dto);
    Task<CategoryDto?> UpdateCategoryAsync(int id, CreateCategoryDto dto);
    Task<bool> DeleteCategoryAsync(int id);
}
