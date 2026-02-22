using BookRatingAPI.DTOs;

namespace BookRatingAPI.Services
{
    public interface IElasticSearchService
    {
        Task Migrate();
        Task<List<BookDto>> GetBooksElastic(string? title, string? author);
        Task<List<BookDto>> GetTopRatedBooks();
        Task<List<BookDto>> GetBooksByCategory(string category);
        Task<List<BookDto>> GetBooksByYear(int year);
        Task<List<BookDto>> RatingFiltering(int rating);
        Task<List<BookDto>> Sort(string sortBy, string sortOrder);
        Task UpsertBook(int id, CreateBookDto book);
        Task DeleteBook(int bookId);
    }
}
