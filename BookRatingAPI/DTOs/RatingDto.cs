namespace BookRatingAPI.DTOs
{
	public class RatingDto
	{
		public int Id { get; set; }
		public int BookId { get; set; }
		public int UserId { get; set; }
		public string Username { get; set; } = string.Empty;
		public int Score { get; set; }
		public string? Comment { get; set; }
		public DateTime CreatedAt { get; set; }
	}
}
