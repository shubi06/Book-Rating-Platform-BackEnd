using System.Collections.Generic;
using System.Threading.Tasks;
using BookRatingAPI.DTOs;

namespace BookRatingAPI.Services
{
    public interface IBookService
    {
        Task<BookDto> CreateBookAsync(CreateBookDto dto);
        Task<BookDto?> GetBookByIdAsync(int id);
        Task<List<BookDto>> GetBooksAsync(string? search, int? categotyId);
        Task<BookDto?> UpdateBookAsync(int id, CreateBookDto dto);
        Task<bool> DeleteBookAsync(int id);
        Task<int> ReindexBooksAsync();
    }
}
