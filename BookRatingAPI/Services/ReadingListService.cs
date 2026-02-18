using System.Threading.Tasks;
using BookRatingAPI.Data;
using BookRatingAPI.DTOs;
using BookRatingAPI.Models;
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
            Status = entry.Status,
            StatusName = entry.Status.ToString(),
            AddedAt = entry.AddedAt
        }, null);
    }
}
