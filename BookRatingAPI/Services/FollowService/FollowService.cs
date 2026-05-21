using BookRatingAPI.Data;
using BookRatingAPI.DTOs.FollowDTOs;
using BookRatingAPI.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BookRatingAPI.Services;

public class FollowService : IFollowService
{
    private readonly AppDbContext _context;
    private readonly ILogger<FollowService> _logger;

    private const int MaxPageSize = 50;
    private const int DefaultPageSize = 20;
    // Hard cap on rows pulled per source during feed assembly. Without this,
    // a request for page=1000&pageSize=50 would pull 50,000 rows twice.
    private const int MaxFeedFetchWindow = 1000;

    public FollowService(AppDbContext context, ILogger<FollowService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<FollowOperationResult> FollowAsync(int followerId, int followeeId)
    {
        if (followerId == followeeId)
        {
            _logger.LogWarning("Self-follow attempt by UserId={UserId}", followerId);
            return FollowOperationResult.SelfFollow;
        }

        var targetExists = await _context.Users.AnyAsync(u => u.Id == followeeId);
        if (!targetExists)
        {
            _logger.LogWarning("Follow target not found: FolloweeId={FolloweeId}", followeeId);
            return FollowOperationResult.UserNotFound;
        }

        var already = await _context.Follows
            .AnyAsync(f => f.FollowerId == followerId && f.FolloweeId == followeeId);
        if (already)
        {
            _logger.LogInformation(
                "Duplicate follow ignored: FollowerId={FollowerId}, FolloweeId={FolloweeId}",
                followerId, followeeId);
            return FollowOperationResult.AlreadyFollowing;
        }

        _context.Follows.Add(new Follow
        {
            FollowerId = followerId,
            FolloweeId = followeeId
        });
        await _context.SaveChangesAsync();

        _logger.LogInformation(
            "Follow created: FollowerId={FollowerId}, FolloweeId={FolloweeId}",
            followerId, followeeId);
        return FollowOperationResult.Success;
    }

    public async Task<FollowOperationResult> UnfollowAsync(int followerId, int followeeId)
    {
        var follow = await _context.Follows
            .FirstOrDefaultAsync(f => f.FollowerId == followerId && f.FolloweeId == followeeId);

        if (follow == null)
        {
            _logger.LogWarning(
                "Unfollow target not found: FollowerId={FollowerId}, FolloweeId={FolloweeId}",
                followerId, followeeId);
            return FollowOperationResult.NotFollowing;
        }

        _context.Follows.Remove(follow);
        await _context.SaveChangesAsync();

        _logger.LogInformation(
            "Follow removed: FollowerId={FollowerId}, FolloweeId={FolloweeId}",
            followerId, followeeId);
        return FollowOperationResult.Success;
    }

    public async Task<List<FollowUserDto>?> GetFollowersAsync(int userId)
    {
        var userExists = await _context.Users.AnyAsync(u => u.Id == userId);
        if (!userExists)
        {
            _logger.LogWarning("Followers requested for missing user: UserId={UserId}", userId);
            return null;
        }

        return await _context.Follows
            .Where(f => f.FolloweeId == userId)
            .OrderByDescending(f => f.CreatedAt)
            .Select(f => new FollowUserDto
            {
                Id = f.Follower.Id,
                Username = f.Follower.Username,
                FollowedAt = f.CreatedAt
            })
            .ToListAsync();
    }

    public async Task<List<FollowUserDto>?> GetFollowingAsync(int userId)
    {
        var userExists = await _context.Users.AnyAsync(u => u.Id == userId);
        if (!userExists)
        {
            _logger.LogWarning("Following requested for missing user: UserId={UserId}", userId);
            return null;
        }

        return await _context.Follows
            .Where(f => f.FollowerId == userId)
            .OrderByDescending(f => f.CreatedAt)
            .Select(f => new FollowUserDto
            {
                Id = f.Followee.Id,
                Username = f.Followee.Username,
                FollowedAt = f.CreatedAt
            })
            .ToListAsync();
    }

    public async Task<FollowStatsDto?> GetFollowStatsAsync(int userId)
    {
        var userExists = await _context.Users.AnyAsync(u => u.Id == userId);
        if (!userExists) return null;

        var followers = await _context.Follows.CountAsync(f => f.FolloweeId == userId);
        var following = await _context.Follows.CountAsync(f => f.FollowerId == userId);

        return new FollowStatsDto
        {
            FollowersCount = followers,
            FollowingCount = following
        };
    }

    public async Task<ActivityFeedDto> GetActivityFeedAsync(int userId, int page, int pageSize)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = DefaultPageSize;
        if (pageSize > MaxPageSize) pageSize = MaxPageSize;

        var followedIds = await _context.Follows
            .Where(f => f.FollowerId == userId)
            .Select(f => f.FolloweeId)
            .ToListAsync();

        if (followedIds.Count == 0)
        {
            _logger.LogInformation("Empty feed (no follows) for UserId={UserId}", userId);
            return new ActivityFeedDto { Page = page, PageSize = pageSize, TotalCount = 0 };
        }

        var ratingsCount = await _context.Ratings.CountAsync(r => followedIds.Contains(r.UserId));
        var readingCount = await _context.ReadingLists.CountAsync(rl => followedIds.Contains(rl.UserId));
        var totalCount = ratingsCount + readingCount;

        // Cap each per-source fetch at the high-water mark for this page; merge in memory.
        // Avoids the cost of a SQL UNION across heterogeneous projections while keeping
        // the working set bounded to ~2 * fetchLimit rows. MaxFeedFetchWindow prevents a
        // very large `page` from forcing oversized reads.
        var fetchLimit = Math.Min(page * pageSize, MaxFeedFetchWindow);

        var ratingItems = await _context.Ratings
            .Where(r => followedIds.Contains(r.UserId))
            .OrderByDescending(r => r.CreatedAt)
            .Take(fetchLimit)
            .Select(r => new ActivityFeedItemDto
            {
                Type = ActivityType.Rating,
                ActorId = r.UserId,
                ActorUsername = r.User.Username,
                BookId = r.BookId,
                BookTitle = r.Book.Title,
                BookAuthor = r.Book.Author,
                CoverImageUrl = r.Book.CoverImageUrl,
                Timestamp = r.CreatedAt,
                Score = r.Score,
                Comment = r.Comment,
                Status = null
            })
            .ToListAsync();

        var readingItems = await _context.ReadingLists
            .Where(rl => followedIds.Contains(rl.UserId))
            .OrderByDescending(rl => rl.AddedAt)
            .Take(fetchLimit)
            .Select(rl => new ActivityFeedItemDto
            {
                Type = ActivityType.ReadingList,
                ActorId = rl.UserId,
                ActorUsername = rl.User.Username,
                BookId = rl.BookId,
                BookTitle = rl.Book.Title,
                BookAuthor = rl.Book.Author,
                CoverImageUrl = rl.Book.CoverImageUrl,
                Timestamp = rl.AddedAt,
                Score = null,
                Comment = null,
                Status = rl.Status
            })
            .ToListAsync();

        var pageItems = ratingItems
            .Concat(readingItems)
            .OrderByDescending(i => i.Timestamp)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        _logger.LogInformation(
            "Feed page built for UserId={UserId}: Page={Page}, PageSize={PageSize}, Returned={Returned}, Total={Total}",
            userId, page, pageSize, pageItems.Count, totalCount);

        return new ActivityFeedDto
        {
            Items = pageItems,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount
        };
    }
}
