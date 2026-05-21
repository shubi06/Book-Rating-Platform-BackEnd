using BookRatingAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace BookRatingAPI.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options) { }

    public DbSet<User> Users { get; set; }
    public DbSet<Book> Books { get; set; }
    public DbSet<Category> Categories { get; set; }
    public DbSet<Rating> Ratings { get; set; }
    public DbSet<ReadingList> ReadingLists { get; set; }
    public DbSet<Follow> Follows { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Rating>().HasIndex(r => new { r.UserId, r.BookId }).IsUnique();

        modelBuilder.Entity<ReadingList>().HasIndex(rl => new { rl.UserId, rl.BookId }).IsUnique();

        modelBuilder.Entity<User>().HasIndex(u => u.Email).IsUnique();

        modelBuilder.Entity<User>().HasIndex(u => u.Username).IsUnique();

        // Self-referential many-to-many through Follow.
        // OnDelete must be Restrict: SQL Server forbids multiple cascade paths from one table.
        modelBuilder.Entity<Follow>(entity =>
        {
            entity.HasIndex(f => new { f.FollowerId, f.FolloweeId }).IsUnique();

            entity.HasOne(f => f.Follower)
                .WithMany(u => u.Following)
                .HasForeignKey(f => f.FollowerId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(f => f.Followee)
                .WithMany(u => u.Followers)
                .HasForeignKey(f => f.FolloweeId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
