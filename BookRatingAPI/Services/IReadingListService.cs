using System.Collections.Generic;
using System.Threading.Tasks;
using BookRatingAPI.DTOs;
using BookRatingAPI.Models.Enums;

namespace BookRatingAPI.Services;

public interface IReadingListService
{
    Task<(ReadingListEntryDto? Entry, string? Error)> AddToReadingListAsync(int userId, AddToReadingListDto dto);
    Task<List<ReadingListEntryDto>> GetReadingListAsync(int userId, ReadingStatus? status);
    Task<(ReadingListEntryDto? Entry, string? Error)> UpdateStatusAsync(int userId, int entryId, UpdateReadingListStatusDto dto);
    Task<bool> RemoveFromReadingListAsync(int userId, int entryId);
}
