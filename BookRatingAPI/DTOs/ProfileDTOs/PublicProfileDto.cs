namespace BookRatingAPI.DTOs.ProfileDTOs;

public class PublicProfileDto
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public ProfileStatsDto Stats { get; set; } = null!;
    public List<RecentRatingDto> RecentRatings { get; set; } = new();
}