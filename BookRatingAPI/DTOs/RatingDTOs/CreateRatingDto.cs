using System.ComponentModel.DataAnnotations;

public class CreateRatingDto
{
	[Required]
	public int BookId { get; set; }

	[Required]
	[Range(1, 5)]
	public int Score { get; set; }

	[MaxLength(1000)]
	public string? Comment { get; set; }
}
