using System;
using System.ComponentModel.DataAnnotations;

namespace BookRatingAPI.DTOs;

public class BookDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Author { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? CoverImageUrl { get; set; }
    public int? PublicationYear { get; set; }
    public string? ISBN { get; set; }
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public double AverageRating { get; set; }
    public int RatingCount { get; set; }
}

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
