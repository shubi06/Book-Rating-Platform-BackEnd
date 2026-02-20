using BookRatingAPI.DTOs;

public class RecentRatingDto
{
    public int Id { get; set; }
    public int Score { get; set; }
    public string? Comment { get; set; }
    public DateTime CreatedAt { get; set; }
    public ProfileBookDto Book { get; set; } = null!;
}