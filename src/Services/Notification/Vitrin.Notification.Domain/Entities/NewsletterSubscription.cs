using Vitrin.Shared.Kernel.Domain;

namespace Vitrin.Notification.Domain.Entities;

public sealed class NewsletterSubscription : Entity
{
    public Guid? UserId { get; private set; }
    public string EmailAddress { get; private set; } = string.Empty;
    public bool DailyLaunches { get; private set; }
    public bool WeeklyRoundup { get; private set; } = true;
    public bool ProductUpdates { get; private set; }
    public bool UpcomingLaunches { get; private set; }
    public bool AiDigest { get; private set; }
    public bool DeveloperDigest { get; private set; }
    public bool IsActive { get; private set; } = true;
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime UpdatedAtUtc { get; private set; }
    private NewsletterSubscription() { }

    public static NewsletterSubscription Create(string emailAddress, Guid? userId = null)
    {
        var now = DateTime.UtcNow;
        return new NewsletterSubscription
        {
            UserId = userId,
            EmailAddress = emailAddress.Trim().ToLowerInvariant(),
            WeeklyRoundup = true,
            IsActive = true,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };
    }

    public void Update(Guid? userId, bool dailyLaunches, bool weeklyRoundup, bool productUpdates,
        bool upcomingLaunches, bool aiDigest, bool developerDigest, bool isActive)
    {
        UserId ??= userId;
        DailyLaunches = dailyLaunches;
        WeeklyRoundup = weeklyRoundup;
        ProductUpdates = productUpdates;
        UpcomingLaunches = upcomingLaunches;
        AiDigest = aiDigest;
        DeveloperDigest = developerDigest;
        IsActive = isActive;
        UpdatedAtUtc = DateTime.UtcNow;
    }
}
