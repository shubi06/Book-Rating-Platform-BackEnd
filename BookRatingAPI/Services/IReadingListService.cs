using System.Threading.Tasks;
using BookRatingAPI.DTOs;
using BookRatingAPI.Models.Enums;

namespace BookRatingAPI.Services;

public interface IReadingListService
{
    Task<(ReadingListEntryDto? Entry, string? Error)> AddToReadingListAsync(int userId, AddToReadingListDto dto);
    Task<List<ReadingListEntryDto>> GetReadingListAsync(int userId, ReadingStatus? status);
}
