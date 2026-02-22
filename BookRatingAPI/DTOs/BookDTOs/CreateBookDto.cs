using System.ComponentModel.DataAnnotations;

namespace BookRatingAPI.DTOs;

public class CreateBookDto
{
    [Required]
    public string Title { get; set; } = string.Empty;

    [Required]
    public string Author { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;
    public string? CoverImageUrl { get; set; }
    public int? PublicationYear { get; set; }
    public string? ISBN { get; set; }

    [Required]
    public int CategoryId { get; set; }
}

