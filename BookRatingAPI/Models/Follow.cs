using System;

namespace BookRatingAPI.Models;

public class Follow
{
    public int Id { get; set; }

    public int FollowerId { get; set; }
    public User Follower { get; set; } = null!;

    public int FolloweeId { get; set; }
    public User Followee { get; set; } = null!;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
