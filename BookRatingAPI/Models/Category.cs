using System.ComponentModel.DataAnnotations;

namespace BookRatingAPI.Models;

public class Category
{
    public int Id { get; set; }
    
    [Required]
    [MaxLength(50)]
    public string Name { get; set; } = string.Empty;
    
    public ICollection<Book> Books { get; set; } = new List<Book>();
}
