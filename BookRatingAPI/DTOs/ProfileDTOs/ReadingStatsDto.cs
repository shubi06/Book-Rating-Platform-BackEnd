namespace BookRatingAPI.DTOs.ProfileDTOs;

public class ReadingStatsDto
{
    public int BooksRead { get; set; }
    public int BooksWantToRead { get; set; }
    public double? AverageRatingGiven { get; set; }
    public string? FavoriteCategory { get; set; }
    public string? MostActiveMonth { get; set; }
}
