using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BookRatingAPI.Data;
using BookRatingAPI.DTOs;
using BookRatingAPI.Models;
using BookRatingAPI.Models.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BookRatingAPI.Services;

/// <summary>
/// Provides operations for managing a user's reading list,
/// including adding, retrieving, updating, and removing entries.
/// </summary>
public class ReadingListService : IReadingListService
{
    private readonly AppDbContext _context;
    private readonly ILogger<ReadingListService> _logger;

    /// <summary>
    /// Initializes a new instance of <see cref="ReadingListService"/>.
    /// </summary>
    /// <param name="context">The database context used to access reading list data.</param>
    /// <param name="logger">The logger instance for this service.</param>
    public ReadingListService(AppDbContext context, ILogger<ReadingListService> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Adds a book to the specified user's reading list.
    /// </summary>
    /// <param name="userId">The ID of the user adding the book.</param>
    /// <param name="dto">The DTO containing the book ID and desired reading status.</param>
    /// <returns>
    /// A tuple with the created <see cref="ReadingListEntryDto"/> on success,
    /// or an error message string if the book was not found or is already in the list.
    /// </returns>
    public async Task<(ReadingListEntryDto? Entry, string? Error)> AddToReadingListAsync(int userId, AddToReadingListDto dto)
    {
        _logger.LogInformation("Adding book to reading list: BookId={BookId}, UserId={UserId}", dto.BookId, userId);
        
        var book = await _context.Books
            .Include(b => b.Category)
            .FirstOrDefaultAsync(b => b.Id == dto.BookId);
            
        if (book == null)
        {
            _logger.LogWarning("Book not found: BookId={BookId}", dto.BookId);
            return (null, "Book not found.");
        }

        var duplicate = await _context.ReadingLists
            .AnyAsync(rl => rl.UserId == userId && rl.BookId == dto.BookId);

        if (duplicate)
        {
            _logger.LogWarning("Duplicate reading list entry: BookId={BookId}, UserId={UserId}", dto.BookId, userId);
            return (null, "This book is already in your reading list.");
        }

        var entry = new ReadingList
        {
            UserId = userId,
            BookId = dto.BookId,
            Status = dto.Status
        };

        _context.ReadingLists.Add(entry);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Book added to reading list successfully: EntryId={EntryId}", entry.Id);
        return (new ReadingListEntryDto
        {
            Id = entry.Id,
            BookId = book.Id,
            BookTitle = book.Title,
            BookAuthor = book.Author,
            CoverImageUrl = book.CoverImageUrl,
            BookCategory = book.Category?.Name ?? string.Empty,
            Status = entry.Status,
            StatusName = entry.Status.ToString(),
            AddedAt = entry.AddedAt
        }, null);
    }

    /// <summary>
    /// Retrieves all reading list entries for a user, optionally filtered by reading status.
    /// </summary>
    /// <param name="userId">The ID of the user whose reading list is being fetched.</param>
    /// <param name="status">An optional status filter; if null, all entries are returned.</param>
    /// <returns>A list of <see cref="ReadingListEntryDto"/> ordered by most recently added.</returns>
    public async Task<List<ReadingListEntryDto>> GetReadingListAsync(int userId, ReadingStatus? status)
    {
        _logger.LogInformation("Fetching reading list for UserId={UserId}, Status={Status}", userId, status);
        
        var query = _context.ReadingLists
            .Include(rl => rl.Book)
            .ThenInclude(b => b.Category)
            .Where(rl => rl.UserId == userId);

        if (status.HasValue)
            query = query.Where(rl => rl.Status == status.Value);

        var entries = await query.OrderByDescending(rl => rl.AddedAt).ToListAsync();
        
        _logger.LogInformation("Retrieved {Count} reading list entries for UserId={UserId}", entries.Count, userId);
        
        return entries.Select(rl => new ReadingListEntryDto
        {
            Id = rl.Id,
            BookId = rl.Book.Id,
            BookTitle = rl.Book.Title,
            BookAuthor = rl.Book.Author,
            CoverImageUrl = rl.Book.CoverImageUrl,
            BookCategory = rl.Book.Category?.Name ?? string.Empty,
            Status = rl.Status,
            StatusName = rl.Status.ToString(),
            AddedAt = rl.AddedAt
        }).ToList();
    }

    /// <summary>
    /// Updates the reading status of an existing reading list entry.
    /// </summary>
    /// <param name="userId">The ID of the user who owns the entry.</param>
    /// <param name="entryId">The ID of the reading list entry to update.</param>
    /// <param name="dto">The DTO containing the new reading status.</param>
    /// <returns>
    /// A tuple with the updated <see cref="ReadingListEntryDto"/> on success,
    /// or an error message string if the entry was not found.
    /// </returns>
    public async Task<(ReadingListEntryDto? Entry, string? Error)> UpdateStatusAsync(int userId, int entryId, UpdateReadingListStatusDto dto)
    {
        _logger.LogInformation("Updating reading list status: EntryId={EntryId}, UserId={UserId}", entryId, userId);
        
        var entry = await _context.ReadingLists
            .Include(rl => rl.Book)
            .ThenInclude(b => b.Category)
            .FirstOrDefaultAsync(rl => rl.Id == entryId && rl.UserId == userId);

        if (entry == null)
        {
            _logger.LogWarning("Reading list entry not found: EntryId={EntryId}, UserId={UserId}", entryId, userId);
            return (null, "Reading list entry not found.");
        }

        entry.Status = dto.Status;
        await _context.SaveChangesAsync();

        _logger.LogInformation("Reading list status updated successfully: EntryId={EntryId}", entryId);
        return (new ReadingListEntryDto
        {
            Id = entry.Id,
            BookId = entry.Book.Id,
            BookTitle = entry.Book.Title,
            BookAuthor = entry.Book.Author,
            CoverImageUrl = entry.Book.CoverImageUrl,
            BookCategory = entry.Book.Category?.Name ?? string.Empty,
            Status = entry.Status,
            StatusName = entry.Status.ToString(),
            AddedAt = entry.AddedAt
        }, null);
    }

    /// <summary>
    /// Removes an entry from the user's reading list.
    /// </summary>
    /// <param name="userId">The ID of the user who owns the entry.</param>
    /// <param name="entryId">The ID of the reading list entry to remove.</param>
    /// <returns><c>true</c> if the entry was removed successfully; <c>false</c> if it was not found.</returns>
    public async Task<bool> RemoveFromReadingListAsync(int userId, int entryId)
    {
        _logger.LogInformation("Removing from reading list: EntryId={EntryId}, UserId={UserId}", entryId, userId);
        
        var entry = await _context.ReadingLists
            .FirstOrDefaultAsync(rl => rl.Id == entryId && rl.UserId == userId);

        if (entry == null)
        {
            _logger.LogWarning("Reading list entry not found: EntryId={EntryId}, UserId={UserId}", entryId, userId);
            return false;
        }

        _context.ReadingLists.Remove(entry);
        await _context.SaveChangesAsync();
        
        _logger.LogInformation("Removed from reading list successfully: EntryId={EntryId}", entryId);
        return true;
    }
}
