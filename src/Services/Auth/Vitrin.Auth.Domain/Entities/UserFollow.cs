using Vitrin.Shared.Kernel.Domain;

namespace Vitrin.Auth.Domain.Entities;

public class UserFollow
{
    public Guid FollowerId { get; private set; }
    public User Follower { get; private set; } = null!;

    public Guid FollowingId { get; private set; }
    public User Following { get; private set; } = null!;

    public DateTime CreatedAt { get; private set; }

    protected UserFollow() { }

    public UserFollow(Guid followerId, Guid followingId)
    {
        FollowerId = followerId;
        FollowingId = followingId;
        CreatedAt = DateTime.UtcNow;
    }
}
