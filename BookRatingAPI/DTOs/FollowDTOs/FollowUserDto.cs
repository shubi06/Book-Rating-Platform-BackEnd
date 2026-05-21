namespace BookRatingAPI.DTOs.FollowDTOs;

public class FollowUserDto
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public DateTime FollowedAt { get; set; }
}
