namespace BookRatingAPI.DTOs.ProfileDTOs;

public class SimilarReaderDto
{
    public int UserId { get; set; }
    public string Username { get; set; } = string.Empty;

    // Jaccard similarity over books both users rated >= 4. Range [0,1], rounded to 4 decimals.
    public double SimilarityScore { get; set; }

    public int OverlapCount { get; set; }

    // True iff the calling user follows this candidate. Applied per-request after cache lookup
    // so two callers viewing the same target share the cached similarity computation.
    public bool IsFollowedByMe { get; set; }
}
