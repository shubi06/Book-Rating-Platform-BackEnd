using BookRatingAPI.Data;
using BookRatingAPI.DTOs;
using BookRatingAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace BookRatingAPI.Services
{
    public class BookService : IBookService
    {
        private readonly ILogger<BookService> _logger;
        private readonly AppDbContext _dbcontext;

        public BookService(ILogger<BookService> logger, AppDbContext dbContext)
        {
            _dbcontext = dbContext;
            _logger = logger;
        }

        public async Task createBookAsync(Book book)
        {
            await _dbcontext.Books.AddAsync(book);
            await _dbcontext.SaveChangesAsync();
            _logger.LogInformation("Book Created Succesfull!");
        }

        public async Task<Book?> getBookByIdAsync(int id)
        {
            var book = await _dbcontext.Books.FindAsync(id);
            if (book == null)
            {
                _logger.LogWarning("Bool Value is Null!");
                return null;
            }
            return book;
        }

        public async Task<ICollection<Book>> getAllBooksAsync()
        {
            var books = await _dbcontext.Books.ToListAsync();
            return books;
        }

        public async Task<Book?> updateBookAsync(int id, UpdateBookDto dto)
        {
            var book = await _dbcontext.Books.FindAsync(id);
            if (book == null)
            {
                _logger.LogWarning("Book info is null!");
                return null;
            }
            book.Title = dto.Title;
            book.Author = dto.Author;
            book.CategoryId = dto.CategoryId;
            book.CoverImageUrl = dto.CoverImageUrl;
            book.Description = dto.Description;
            book.PublicationYear = dto.PublicationYear;
            book.ISBN = dto.ISBN;

            await _dbcontext.SaveChangesAsync();
            _logger.LogInformation($"Book {id} updatet Succesfully!");

            return book;
        }

        public async Task<bool> deleteBookAsync(int id)
        {
            var delBook = await _dbcontext.Books.FindAsync(id);

            if (delBook == null)
            {
                _logger.LogWarning("Book does not exist!");
                return false;
            }

            _dbcontext.Books.Remove(delBook);
            await _dbcontext.SaveChangesAsync();
            _logger.LogInformation($"Book {id} deleted Succesfully!");

            return true;
        }
    }
}
