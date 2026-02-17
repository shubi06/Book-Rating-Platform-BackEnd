using System.ComponentModel.DataAnnotations;

namespace BookRatingAPI.DTOs.AuthDTOs
{
	public class CreateRatingDto
	{
		[Range(1, 5)]
		public int Stars { get; set; }

		[MaxLength(1000)]
		public string? Comment { get; set; }

		public int BookId { get; set; }
	}
}
