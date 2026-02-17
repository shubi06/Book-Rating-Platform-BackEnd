using System.ComponentModel.DataAnnotations;

namespace BookRatingAPI.Models.Models
{
	public class Rating
	{
		public int Id { get; set; }

		[Range(1, 5)]
		public int Stars { get; set; }

		[MaxLength(1000)]
		public string? Comment { get; set; }

		public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

		// Relationships
		public int UserId { get; set; }
		public User User { get; set; }

		public int BookId { get; set; }
		public Book Book { get; set; }
	}
}
