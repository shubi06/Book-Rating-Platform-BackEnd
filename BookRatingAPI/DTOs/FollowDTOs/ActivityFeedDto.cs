namespace BookRatingAPI.DTOs.FollowDTOs;

public class ActivityFeedDto
{
    public List<ActivityFeedItemDto> Items { get; set; } = new();
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
    public bool HasMore => (Page * PageSize) < TotalCount;
}
