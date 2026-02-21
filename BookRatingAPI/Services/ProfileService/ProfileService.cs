using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using BookRatingAPI.Data;
using BookRatingAPI.DTOs;
using BookRatingAPI.DTOs.ProfileDTOs;

namespace BookRatingAPI.Services;

public class ProfileService : IProfileService
{
    private readonly AppDbContext _context;

    public ProfileService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<ProfileDto?> GetMyProfileAsync(int userId)
    {
        var user = await _context.Users
            .Include(u => u.Ratings)
            .Include(u => u.ReadingLists)
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (user == null)
            return null;

        return new ProfileDto
        {
            Id = user.Id,
            Username = user.Username,
            Email = user.Email,
            CreatedAt = user.CreatedAt,
            Stats = new ProfileStatsDto
            {
                TotalRatings = user.Ratings.Count,
                //Calculate average score; default 0 if no ratings exist
                AverageRating = user.Ratings.Any() ? user.Ratings.Average(r => r.Score) : 0,
                BooksInReadingList = user.ReadingLists.Count
            }
        };
    }

    public async Task<PublicProfileDto?> GetUserProfileAsync(int userId)
    {
        var user = await _context.Users
            .Include(u => u.Ratings)
            .ThenInclude(r => r.Book)
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (user == null)
            return null;

        return new PublicProfileDto
        {
            Id = user.Id,
            Username = user.Username,
            CreatedAt = user.CreatedAt,
            Stats = new ProfileStatsDto
            {
                TotalRatings = user.Ratings.Count,
                AverageRating = user.Ratings.Any() ? user.Ratings.Average(r => r.Score) : 0,
                BooksInReadingList = 0 // Don't expose reading list count publicly
            },
            //Map only the 5 most recent ratings
            RecentRatings = user.Ratings
                .OrderByDescending(r => r.CreatedAt)
                .Take(5)
                .Select(r => new RecentRatingDto
                {
                    Id = r.Id,
                    Score = r.Score,
                    Comment = r.Comment,
                    CreatedAt = r.CreatedAt,
                    Book = new ProfileBookDto
                    {
                        Id = r.Book.Id,
                        Title = r.Book.Title,
                        Author = r.Book.Author
                    }
                })
                .ToList()
        };
    }
}
