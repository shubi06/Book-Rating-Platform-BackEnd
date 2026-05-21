using BookRatingAPI.DTOs.ProfileDTOs;

namespace BookRatingAPI.Services;

public interface ISimilarReadersService
{
    // Returns null when targetUserId does not exist (controller maps to 404).
    // Returns an empty list when the target has fewer than the minimum high-rated books
    // required to compute a meaningful neighborhood (cold-start case, HTTP 200).
    Task<List<SimilarReaderDto>?> GetSimilarReadersAsync(int targetUserId, int callerUserId, int limit);
}
