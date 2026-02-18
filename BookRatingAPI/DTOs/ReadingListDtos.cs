using System;
using System.ComponentModel.DataAnnotations;
using BookRatingAPI.Models.Enums;

namespace BookRatingAPI.DTOs;

public class AddToReadingListDto
{
    [Required]
    public int BookId { get; set; }

    [Required]
    public ReadingStatus Status { get; set; }
}

public class ReadingListEntryDto
{
    public int Id { get; set; }
    public int BookId { get; set; }
    public string BookTitle { get; set; } = string.Empty;
    public string BookAuthor { get; set; } = string.Empty;
    public string? CoverImageUrl { get; set; }
    public ReadingStatus Status { get; set; }
    public string StatusName { get; set; } = string.Empty;
    public DateTime AddedAt { get; set; }
}
