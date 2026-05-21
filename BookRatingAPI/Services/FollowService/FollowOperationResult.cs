namespace BookRatingAPI.Services;

public enum FollowOperationResult
{
    Success,
    UserNotFound,
    SelfFollow,
    AlreadyFollowing,
    NotFollowing
}
