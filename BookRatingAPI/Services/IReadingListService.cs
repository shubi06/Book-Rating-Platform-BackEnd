using System.Threading.Tasks;
using BookRatingAPI.DTOs;

namespace BookRatingAPI.Services;

public interface IReadingListService
{
    Task<(ReadingListEntryDto? Entry, string? Error)> AddToReadingListAsync(int userId, AddToReadingListDto dto);
}
