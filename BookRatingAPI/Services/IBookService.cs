using BookRatingAPI.DTOs;
using BookRatingAPI.Models;

namespace BookRatingAPI.Services
{
    public interface IBookService
    {
        Task createBookAsync(Book book);
        Task<Book?> getBookByIdAsync(int id);
        Task<ICollection<Book>> getAllBooksAsync();
        Task<Book?> updateBookAsync(int id, UpdateBookDto dto);
        Task<bool> deleteBookAsync(int id);
    }
}
