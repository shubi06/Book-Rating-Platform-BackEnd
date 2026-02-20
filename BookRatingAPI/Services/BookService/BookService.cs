using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BookRatingAPI.Data;
using BookRatingAPI.DTOs;
using BookRatingAPI.DTOs.BookDTOs;
using BookRatingAPI.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace BookRatingAPI.Services;

/// <summary>
/// Service for managing books and their related operations
/// </summary>
public class BookService : IBookService
{
    private readonly AppDbContext _context;
    private readonly IMemoryCache _cache;
    private const int CacheExpirationMinutes = 5;
    private const int BookCacheExpirationMinutes = 10;

    public BookService(AppDbContext context, IMemoryCache cache)
    {
        _context = context;
        _cache = cache;
    }

    /// <summary>
    /// Get all books with optional search and category filtering
    /// </summary>
    /// <param name="search">Search term for title, author, or description</param>
    /// <param name="categoryId">Optional category ID to filter by</param>
    /// <returns>List of books matching the criteria</returns>
    public async Task<List<BookDto>> GetBooksAsync(string? search, int? categoryId)
    {
        // If search is provided, bypass cache and search directly
        if (!string.IsNullOrEmpty(search))
        {
            return await SearchBooksAsync(search);
        }

        // Try to get from cache
        var cacheKey = GenerateCacheKey(categoryId);
        if (_cache.TryGetValue(cacheKey, out List<BookDto>? cachedBooks) && cachedBooks != null)
        {
            return cachedBooks;
        }

        // Query database
        var query = _context.Books
            .Include(b => b.Category)
            .Include(b => b.Ratings)
            .AsQueryable();

        if (categoryId.HasValue)
        {
            query = query.Where(b => b.CategoryId == categoryId);
        }

        var result = await query.Select(b => MapToDto(b)).ToListAsync();

        // Cache the result
        _cache.Set(cacheKey, result, TimeSpan.FromMinutes(CacheExpirationMinutes));

        return result;
    }

    /// <summary>
    /// Get a specific book by ID
    /// </summary>
    /// <param name="id">The ID of the book</param>
    /// <returns>The book if found, null otherwise</returns>
    public async Task<BookDto?> GetBookByIdAsync(int id)
    {
        var cacheKey = $"book:{id}";

        // Try to get from cache
        if (_cache.TryGetValue(cacheKey, out BookDto? cachedBook) && cachedBook != null)
        {
            return cachedBook;
        }

        // Query database
        var book = await _context.Books
            .Include(b => b.Category)
            .Include(b => b.Ratings)
            .FirstOrDefaultAsync(b => b.Id == id);

        if (book == null)
        {
            return null;
        }

        var result = MapToDto(book);

        // Cache the result
        _cache.Set(cacheKey, result, TimeSpan.FromMinutes(BookCacheExpirationMinutes));

        return result;
    }

    /// <summary>
    /// Create a new book
    /// </summary>
    /// <param name="dto">Book creation data</param>
    /// <returns>The created book</returns>
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

        // Load category for DTO mapping
        await _context.Entry(book).Reference(b => b.Category).LoadAsync();

        // Clear cache after modification
        ClearBooksCache();

        return MapToDto(book);
    }

    /// <summary>
    /// Update an existing book
    /// </summary>
    /// <param name="id">The ID of the book to update</param>
    /// <param name="dto">Updated book data</param>
    /// <returns>The updated book if found, null otherwise</returns>
    public async Task<BookDto?> UpdateBookAsync(int id, CreateBookDto dto)
    {
        var book = await _context.Books.FindAsync(id);

        if (book == null)
        {
            return null;
        }

        // Update book properties
        book.Title = dto.Title;
        book.Author = dto.Author;
        book.Description = dto.Description;
        book.CoverImageUrl = dto.CoverImageUrl;
        book.PublicationYear = dto.PublicationYear;
        book.ISBN = dto.ISBN;
        book.CategoryId = dto.CategoryId;

        await _context.SaveChangesAsync();

        // Clear cache after modification
        _cache.Remove($"book:{id}");
        ClearBooksCache();

        // Reload with related data
        var updatedBook = await _context.Books
            .Include(b => b.Category)
            .Include(b => b.Ratings)
            .FirstOrDefaultAsync(b => b.Id == id);

        return updatedBook != null ? MapToDto(updatedBook) : null;
    }

    /// <summary>
    /// Delete a book
    /// </summary>
    /// <param name="id">The ID of the book to delete</param>
    /// <returns>True if deleted successfully, false if not found</returns>
    public async Task<bool> DeleteBookAsync(int id)
    {
        var book = await _context.Books.FindAsync(id);

        if (book == null)
        {
            return false;
        }

        _context.Books.Remove(book);
        await _context.SaveChangesAsync();

        // Clear cache after modification
        _cache.Remove($"book:{id}");
        ClearBooksCache();

        return true;
    }

    /// <summary>
    /// Reindex books (clears cache)
    /// </summary>
    /// <returns>Task completion</returns>
    public Task<int> ReindexBooksAsync()
    {
        ClearBooksCache();
        return Task.FromResult(0);
    }

    /// <summary>
    /// Search books by title, author, or description
    /// </summary>
    /// <param name="search">Search term</param>
    /// <returns>List of matching books</returns>
    private async Task<List<BookDto>> SearchBooksAsync(string search)
    {
        var books = await _context.Books
            .Include(b => b.Category)
            .Include(b => b.Ratings)
            .Where(b =>
                EF.Functions.Like(b.Title, $"%{search}%") ||
                EF.Functions.Like(b.Author, $"%{search}%") ||
                EF.Functions.Like(b.Description, $"%{search}%")
            )
            .ToListAsync();

        return books.Select(MapToDto).ToList();
    }

    /// <summary>
    /// Clear all books cache entries
    /// </summary>
    private void ClearBooksCache()
    {
        _cache.Remove("books:all");
        // Note: Category-specific caches would need to be tracked separately
        // for complete cache invalidation
    }

    /// <summary>
    /// Generate cache key based on category filter
    /// </summary>
    /// <param name="categoryId">Optional category ID</param>
    /// <returns>Cache key string</returns>
    private static string GenerateCacheKey(int? categoryId)
    {
        return categoryId.HasValue ? $"books:category:{categoryId}" : "books:all";
    }

    /// <summary>
    /// Map a Book entity to a BookDto
    /// </summary>
    /// <param name="book">The book entity</param>
    /// <returns>The mapped DTO</returns>
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
