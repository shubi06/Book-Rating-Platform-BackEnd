using System.ComponentModel.DataAnnotations;

namespace BookRatingAPI.DTOs;

public class CategoryDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

