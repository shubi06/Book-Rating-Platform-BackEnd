namespace BookRatingAPI.DTOs;

public class ProfileDto
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public ProfileStatsDto Stats { get; set; } = null!;
}

public class PublicProfileDto
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public ProfileStatsDto Stats { get; set; } = null!;
    public List<RecentRatingDto> RecentRatings { get; set; } = new();
}

public class ProfileStatsDto
{
    public int TotalRatings { get; set; }
    public double AverageRating { get; set; }
    public int BooksInReadingList { get; set; }
}

public class RecentRatingDto
{
    public int Id { get; set; }
    public int Score { get; set; }
    public string? Comment { get; set; }
    public DateTime CreatedAt { get; set; }
    public ProfileBookDto Book { get; set; } = null!;
}

public class ProfileBookDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Author { get; set; } = string.Empty;
}
