using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BookRatingAPI.Data;
using BookRatingAPI.DTOs;
using BookRatingAPI.Models;
using BookRatingAPI.Models.Enums;
using Microsoft.EntityFrameworkCore;

namespace BookRatingAPI.Services;

public class ReadingListService : IReadingListService
{
    private readonly AppDbContext _context;

    public ReadingListService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<(ReadingListEntryDto? Entry, string? Error)> AddToReadingListAsync(int userId, AddToReadingListDto dto)
    {
        var bookExists = await _context.Books.AnyAsync(b => b.Id == dto.BookId);
        if (!bookExists)
            return (null, "Book not found.");

        var duplicate = await _context.ReadingLists
            .AnyAsync(rl => rl.UserId == userId && rl.BookId == dto.BookId);

        if (duplicate)
            return (null, "This book is already in your reading list.");

        var entry = new ReadingList
        {
            UserId = userId,
            BookId = dto.BookId,
            Status = dto.Status
        };

        _context.ReadingLists.Add(entry);
        await _context.SaveChangesAsync();

        var book = await _context.Books.FindAsync(dto.BookId);

        return (new ReadingListEntryDto
        {
            Id = entry.Id,
            BookId = book!.Id,
            BookTitle = book.Title,
            BookAuthor = book.Author,
            CoverImageUrl = book.CoverImageUrl,
            BookCategory = book.Category?.Name ?? string.Empty,
            Status = entry.Status,
            StatusName = entry.Status.ToString(),
            AddedAt = entry.AddedAt
        }, null);
    }

    public async Task<List<ReadingListEntryDto>> GetReadingListAsync(int userId, ReadingStatus? status)
    {
        var query = _context.ReadingLists
            .Include(rl => rl.Book)
                .ThenInclude(b => b.Category)
            .Where(rl => rl.UserId == userId);

        if (status.HasValue)
            query = query.Where(rl => rl.Status == status.Value);

        var entries = await query
            .OrderByDescending(rl => rl.AddedAt)
            .ToListAsync();

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

    public async Task<(ReadingListEntryDto? Entry, string? Error)> UpdateStatusAsync(int userId, int entryId, UpdateReadingListStatusDto dto)
    {
        var entry = await _context.ReadingLists
            .Include(rl => rl.Book)
                .ThenInclude(b => b.Category)
            .FirstOrDefaultAsync(rl => rl.Id == entryId && rl.UserId == userId);

        if (entry == null)
            return (null, "Reading list entry not found.");

        entry.Status = dto.Status;
        await _context.SaveChangesAsync();

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

    public async Task<bool> RemoveFromReadingListAsync(int userId, int entryId)
    {
        var entry = await _context.ReadingLists
            .FirstOrDefaultAsync(rl => rl.Id == entryId && rl.UserId == userId);

        if (entry == null)
            return false;

        _context.ReadingLists.Remove(entry);
        await _context.SaveChangesAsync();
        return true;
    }
}
