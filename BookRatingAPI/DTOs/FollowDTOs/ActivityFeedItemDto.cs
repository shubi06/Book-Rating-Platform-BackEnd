using BookRatingAPI.Models.Enums;

namespace BookRatingAPI.DTOs.FollowDTOs;

public class ActivityFeedItemDto
{
    public ActivityType Type { get; set; }
    public string TypeName => Type.ToString();

    public int ActorId { get; set; }
    public string ActorUsername { get; set; } = string.Empty;

    public int BookId { get; set; }
    public string BookTitle { get; set; } = string.Empty;
    public string BookAuthor { get; set; } = string.Empty;
    public string? CoverImageUrl { get; set; }

    public DateTime Timestamp { get; set; }

    // Populated when Type == Rating
    public int? Score { get; set; }
    public string? Comment { get; set; }

    // Populated when Type == ReadingList
    public ReadingStatus? Status { get; set; }
    public string? StatusName => Status?.ToString();
}
