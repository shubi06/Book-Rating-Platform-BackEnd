namespace BookRatingAPI.DTOs.ProfileDTOs;

public class ProfileStatsDto
{
    public int TotalRatings { get; set; }
    public double AverageRating { get; set; }
    public int BooksInReadingList { get; set; }
}