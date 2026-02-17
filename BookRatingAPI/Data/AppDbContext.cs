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

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Rating>().HasIndex(r => new { r.UserId, r.BookId }).IsUnique();

        modelBuilder.Entity<Book>(entity =>
        {
            entity.ToTable("Books");

            // Primary Key
            entity.HasKey(b => b.Id);

            // Properties
            entity.Property(b => b.Title).IsRequired().HasMaxLength(200);

            entity.Property(b => b.Author).IsRequired().HasMaxLength(100);

            entity.Property(b => b.Description).HasColumnType("text"); // optional, useful for long descriptions

            entity.Property(b => b.CoverImageUrl).HasMaxLength(500);

            entity.Property(b => b.PublicationYear);

            entity.Property(b => b.ISBN).HasMaxLength(20); // adjust if needed

            entity.Property(b => b.CreatedAt).HasDefaultValueSql("GETUTCDATE()"); // Use NOW() for PostgreSQL

            // Relationships

            // Book → Category (Many-to-One)
            entity
                .HasOne(b => b.Category)
                .WithMany(c => c.Books)
                .HasForeignKey(b => b.CategoryId)
                .OnDelete(DeleteBehavior.Cascade);

            // Book → Ratings (One-to-Many)
            entity
                .HasMany(b => b.Ratings)
                .WithOne(r => r.Book)
                .HasForeignKey(r => r.BookId)
                .OnDelete(DeleteBehavior.Cascade);

            // Book → ReadingLists (Many-to-Many or One-to-Many depending on your model)
            entity
                .HasMany(b => b.ReadingLists)
                .WithOne(rl => rl.Book)
                .HasForeignKey(rl => rl.BookId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Category>(entity =>
        {
            entity.ToTable("Categories");

            // Primary Key
            entity.HasKey(c => c.Id);

            // Properties
            entity.Property(c => c.Name).IsRequired().HasMaxLength(50);

            // Optional but Recommended
            entity.HasIndex(c => c.Name).IsUnique();

            // One-to-Many Relationship (Category -> Books)
            entity
                .HasMany(c => c.Books)
                .WithOne(b => b.Category)
                .HasForeignKey(b => b.CategoryId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ReadingList>().HasIndex(rl => new { rl.UserId, rl.BookId }).IsUnique();

        modelBuilder.Entity<User>().HasIndex(u => u.Email).IsUnique();

        modelBuilder.Entity<User>().HasIndex(u => u.Username).IsUnique();
    }
}
