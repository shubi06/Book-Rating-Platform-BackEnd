using System.ComponentModel.DataAnnotations;

namespace BookRatingAPI.DTOs;

public class UpdateBookDto
{
    [Required]
    public int Id { get; set; } // Required to know which book to update

    public string? Title { get; set; }
    public string? Author { get; set; }
    public string? Description { get; set; }
    public string? CoverImageUrl { get; set; }
    public int? PublicationYear { get; set; }
    public string? ISBN { get; set; }
    public int? CategoryId { get; set; }
}
