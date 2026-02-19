using System.ComponentModel.DataAnnotations;

namespace BookRatingAPI.Models;

public class Rating
{
	public int Id { get; set; }

	public int UserId { get; set; }
	public User User { get; set; } = null!;

	public int BookId { get; set; }
	public Book Book { get; set; } = null!;

	[Range(1, 5)]
	public int Score { get; set; }

	[MaxLength(1000)]
	public string? Comment { get; set; }

	public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
	public DateTime? UpdatedAt { get; set; }
}
