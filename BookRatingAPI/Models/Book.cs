using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace BookRatingAPI.Models;

public class Book
{
    public int Id { get; set; }

    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string Author { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public string? CoverImageUrl { get; set; }

    public int? PublicationYear { get; set; }

    public string? ISBN { get; set; }

    public int CategoryId { get; set; }
    public Category Category { get; set; } = null!;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<Rating> Ratings { get; set; } = new List<Rating>();
    public ICollection<ReadingList> ReadingLists { get; set; } = new List<ReadingList>();
}
