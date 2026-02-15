using BookRatingAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace BookRatingAPI.Data;

public class AppDbContext : DbContext
{
    public DbSet<User> Users { get; set; }
}