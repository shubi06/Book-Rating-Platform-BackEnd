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

// Handles all reading list operations: add, get, update status, and remove.
public class ReadingListService : IReadingListService
{
    private readonly AppDbContext _context;
    private readonly ILogger<ReadingListService> _logger;

    // Injects the db context and logger.
    public ReadingListService(AppDbContext context, ILogger<ReadingListService> logger)
    {
        _context = context;
        _logger = logger;
    }

    // Adds a book to the user's reading list. Returns an error if the book doesn't exist or is already added.
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

    // Gets the user's reading list. Optionally filter by status; returns all entries if no status is given.
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

    // Updates the status of a reading list entry. Returns an error if the entry isn't found.
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

    // Removes a book from the user's reading list. Returns false if the entry doesn't exist.
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
