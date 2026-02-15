using BookRatingAPI.Models.Enums;

namespace BookRatingAPI.Models;

public class ReadingList
{
    public int Id { get; set; }
    
    public int UserId { get; set; }
    public User User { get; set; } = null!;
    
    public int BookId { get; set; }
    public Book Book { get; set; } = null!;
    
    public ReadingStatus Status { get; set; }
    
    public DateTime AddedAt { get; set; } = DateTime.UtcNow;
}
