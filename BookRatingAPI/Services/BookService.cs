using BookRatingAPI.Data;
using BookRatingAPI.DTOs;
using BookRatingAPI.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace BookRatingAPI.Services;

public class BookService : IBookService
{
    private readonly AppDbContext _context;
    private readonly IMemoryCache _cache;

    public BookService(AppDbContext context, IMemoryCache cache)
    {
        _context = context;
        _cache = cache;
    }

    public async Task<List<BookDto>> GetBooksAsync(string? search, int? categoryId)
    {
        if (!string.IsNullOrEmpty(search))
        {
            var books = await _context
                .Books.Include(b => b.Category)
                .Include(b => b.Ratings)
                .Where(b =>
                    EF.Functions.Like(b.Title, $"%{search}%")
                    || EF.Functions.Like(b.Author, $"%{search}%")
                    || EF.Functions.Like(b.Description, $"%{search}%")
                )
                .ToListAsync();

            return books.Select(MapToDto).ToList();
        }

        var cacheKey = categoryId.HasValue ? $"books:category:{categoryId}" : "books:all";

        if (_cache.TryGetValue(cacheKey, out List<BookDto>? cachedBooks) && cachedBooks != null)
            return cachedBooks;

        var query = _context.Books.Include(b => b.Category).Include(b => b.Ratings).AsQueryable();

        if (categoryId.HasValue)
            query = query.Where(b => b.CategoryId == categoryId);

        var result = await query.Select(b => MapToDto(b)).ToListAsync();

        _cache.Set(cacheKey, result, TimeSpan.FromMinutes(5));

        return result;
    }

    public async Task<BookDto?> GetBookByIdAsync(int id)
    {
        var cacheKey = $"book:{id}";

        if (_cache.TryGetValue(cacheKey, out BookDto? cachedBook) && cachedBook != null)
            return cachedBook;

        var book = await _context
            .Books.Include(b => b.Category)
            .Include(b => b.Ratings)
            .FirstOrDefaultAsync(b => b.Id == id);

        if (book == null)
            return null;

        var result = MapToDto(book);

        _cache.Set(cacheKey, result, TimeSpan.FromMinutes(10));

        return result;
    }

    public async Task<BookDto> CreateBookAsync(CreateBookDto dto)
    {
        var book = new Book
        {
            Title = dto.Title,
            Author = dto.Author,
            Description = dto.Description,
            CoverImageUrl = dto.CoverImageUrl,
            PublicationYear = dto.PublicationYear,
            ISBN = dto.ISBN,
            CategoryId = dto.CategoryId,
        };

        _context.Books.Add(book);
        await _context.SaveChangesAsync();

        await _context.Entry(book).Reference(b => b.Category).LoadAsync();

        ClearBooksCache();

        return MapToDto(book);
    }

    public async Task<BookDto?> UpdateBookAsync(int id, CreateBookDto dto)
    {
        var book = await _context.Books.FindAsync(id);

        if (book == null)
            return null;

        book.Title = dto.Title;
        book.Author = dto.Author;
        book.Description = dto.Description;
        book.CoverImageUrl = dto.CoverImageUrl;
        book.PublicationYear = dto.PublicationYear;
        book.ISBN = dto.ISBN;
        book.CategoryId = dto.CategoryId;

        await _context.SaveChangesAsync();

        _cache.Remove($"book:{id}");
        ClearBooksCache();

        var updatedBook = await _context
            .Books.Include(b => b.Category)
            .Include(b => b.Ratings)
            .FirstOrDefaultAsync(b => b.Id == id);

        return updatedBook != null ? MapToDto(updatedBook) : null;
    }

    public async Task<bool> DeleteBookAsync(int id)
    {
        var book = await _context.Books.FindAsync(id);

        if (book == null)
            return false;

        _context.Books.Remove(book);
        await _context.SaveChangesAsync();

        _cache.Remove($"book:{id}");
        ClearBooksCache();

        return true;
    }

    public Task<int> ReindexBooksAsync()
    {
        ClearBooksCache();
        return Task.FromResult(0);
    }

    private void ClearBooksCache()
    {
        _cache.Remove("books:all");
    }

    private static BookDto MapToDto(Book book)
    {
        return new BookDto
        {
            Id = book.Id,
            Title = book.Title,
            Author = book.Author,
            Description = book.Description,
            CoverImageUrl = book.CoverImageUrl,
            PublicationYear = book.PublicationYear,
            ISBN = book.ISBN,
            CategoryId = book.CategoryId,
            CategoryName = book.Category.Name,
            AverageRating = book.Ratings.Any() ? book.Ratings.Average(r => r.Score) : 0,
            RatingCount = book.Ratings.Count,
        };
    }
}
