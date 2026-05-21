using BookRatingAPI.Data;
using BookRatingAPI.DTOs.ProfileDTOs;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace BookRatingAPI.Services;

public class SimilarReadersService : ISimilarReadersService
{
    private readonly AppDbContext _context;
    private readonly IMemoryCache _cache;
    private readonly ILogger<SimilarReadersService> _logger;

    // A "high" rating: 4 or 5 stars. Used as the membership predicate for the Jaccard sets.
    private const int MinScore = 4;

    // A user must have at least this many high ratings of their own to be considered
    // either a meaningful target or a candidate neighbor. Filters out trivial neighborhoods.
    private const int MinUserRatings = 5;

    private const int CacheTtlMinutes = 10;

    // Cached value (without per-caller IsFollowedByMe) so two callers viewing the same
    // target share a single similarity computation.
    private sealed record SimilarReaderRaw(int UserId, string Username, double SimilarityScore, int OverlapCount);

    public SimilarReadersService(
        AppDbContext context,
        IMemoryCache cache,
        ILogger<SimilarReadersService> logger)
    {
        _context = context;
        _cache = cache;
        _logger = logger;
    }

    public async Task<List<SimilarReaderDto>?> GetSimilarReadersAsync(int targetUserId, int callerUserId, int limit)
    {
        var targetExists = await _context.Users.AnyAsync(u => u.Id == targetUserId);
        if (!targetExists)
        {
            _logger.LogWarning("Similar readers: target not found UserId={TargetUserId}", targetUserId);
            return null;
        }

        var cacheKey = $"similar-readers:{targetUserId}:limit:{limit}";

        if (_cache.TryGetValue(cacheKey, out List<SimilarReaderRaw>? cached) && cached != null)
        {
            _logger.LogInformation(
                "Cache hit for similar readers TargetUserId={TargetUserId}, Limit={Limit}",
                targetUserId, limit);
            return await ProjectWithFollowAsync(cached, callerUserId);
        }

        // 1) Pull the target's high-rated book set in a single SQL roundtrip.
        var targetBookIds = await _context.Ratings
            .Where(r => r.UserId == targetUserId && r.Score >= MinScore)
            .Select(r => r.BookId)
            .ToListAsync();

        // Cold-start: target hasn't rated enough books >= MinScore to anchor a neighborhood.
        // Cache the empty result so we don't recompute on every call.
        if (targetBookIds.Count < MinUserRatings)
        {
            _logger.LogInformation(
                "Similar readers cold-start (target has {Count} high ratings) for TargetUserId={TargetUserId}",
                targetBookIds.Count, targetUserId);

            var empty = new List<SimilarReaderRaw>();
            _cache.Set(cacheKey, empty, TimeSpan.FromMinutes(CacheTtlMinutes));
            return new List<SimilarReaderDto>();
        }

        var targetSet = new HashSet<int>(targetBookIds);

        // 2) Candidate prefilter: only users who share at least one of the target's high-rated books.
        //    Done in SQL via Contains over the (typically small) targetBookIds set.
        var candidateUserIds = await _context.Ratings
            .Where(r => r.Score >= MinScore
                        && r.UserId != targetUserId
                        && targetBookIds.Contains(r.BookId))
            .Select(r => r.UserId)
            .Distinct()
            .ToListAsync();

        if (candidateUserIds.Count == 0)
        {
            var empty = new List<SimilarReaderRaw>();
            _cache.Set(cacheKey, empty, TimeSpan.FromMinutes(CacheTtlMinutes));
            return new List<SimilarReaderDto>();
        }

        // 3) Pull the full high-rated set for each candidate plus their username, in one query.
        //    Working set is bounded to candidates only, not the full Ratings table.
        var candidateRatings = await _context.Ratings
            .Where(r => r.Score >= MinScore && candidateUserIds.Contains(r.UserId))
            .Select(r => new { r.UserId, r.BookId, Username = r.User.Username })
            .ToListAsync();

        // 4) Group by candidate, compute Jaccard, apply MinUserRatings on the candidate side.
        var ranked = candidateRatings
            .GroupBy(x => new { x.UserId, x.Username })
            .Select(g =>
            {
                var candidateSet = g.Select(x => x.BookId).ToHashSet();
                if (candidateSet.Count < MinUserRatings)
                {
                    return null;
                }

                var overlap = candidateSet.Count(b => targetSet.Contains(b));
                if (overlap == 0)
                {
                    return null;
                }

                var unionCount = candidateSet.Count + targetSet.Count - overlap;
                var score = unionCount == 0 ? 0.0 : (double)overlap / unionCount;
                var rounded = Math.Round(score, 4, MidpointRounding.AwayFromZero);

                return new SimilarReaderRaw(g.Key.UserId, g.Key.Username, rounded, overlap);
            })
            .Where(x => x != null)
            .Select(x => x!)
            .OrderByDescending(x => x.SimilarityScore)
            .ThenByDescending(x => x.OverlapCount)
            .ThenBy(x => x.UserId)
            .Take(limit)
            .ToList();

        _cache.Set(cacheKey, ranked, TimeSpan.FromMinutes(CacheTtlMinutes));
        _logger.LogInformation(
            "Similar readers computed for TargetUserId={TargetUserId}, Limit={Limit}, Returned={Count}",
            targetUserId, limit, ranked.Count);

        return await ProjectWithFollowAsync(ranked, callerUserId);
    }

    // Per-request fan-out: pull the caller's follow edges that touch this result set in one query,
    // then map the cached raw rows to DTOs.
    private async Task<List<SimilarReaderDto>> ProjectWithFollowAsync(List<SimilarReaderRaw> rows, int callerUserId)
    {
        if (rows.Count == 0)
        {
            return new List<SimilarReaderDto>();
        }

        var candidateIds = rows.Select(r => r.UserId).ToList();
        var followed = await _context.Follows
            .Where(f => f.FollowerId == callerUserId && candidateIds.Contains(f.FolloweeId))
            .Select(f => f.FolloweeId)
            .ToListAsync();

        var followedSet = new HashSet<int>(followed);

        return rows.Select(r => new SimilarReaderDto
        {
            UserId = r.UserId,
            Username = r.Username,
            SimilarityScore = r.SimilarityScore,
            OverlapCount = r.OverlapCount,
            IsFollowedByMe = followedSet.Contains(r.UserId)
        }).ToList();
    }
}
