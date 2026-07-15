using Vitrin.Shared.Kernel.Domain;

namespace Vitrin.Ai.Domain.Entities;

public sealed class AiUsageQuota : Entity
{
    public Guid UserId { get; private set; }
    public DateTime PeriodStartUtc { get; private set; }
    public int RequestCount { get; private set; }
    public DateTime LastRequestedAtUtc { get; private set; }

    private AiUsageQuota()
    {
    }

    public static AiUsageQuota Create(Guid userId, DateTime periodStartUtc, DateTime requestedAtUtc)
    {
        if (userId == Guid.Empty)
        {
            throw new ArgumentException("A user id is required.", nameof(userId));
        }

        return new AiUsageQuota
        {
            UserId = userId,
            PeriodStartUtc = DateTime.SpecifyKind(periodStartUtc.Date, DateTimeKind.Utc),
            RequestCount = 1,
            LastRequestedAtUtc = DateTime.SpecifyKind(requestedAtUtc, DateTimeKind.Utc)
        };
    }

    public bool TryConsume(int dailyLimit, DateTime requestedAtUtc)
    {
        if (dailyLimit <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(dailyLimit));
        }

        if (RequestCount >= dailyLimit)
        {
            return false;
        }

        RequestCount++;
        LastRequestedAtUtc = DateTime.SpecifyKind(requestedAtUtc, DateTimeKind.Utc);
        return true;
    }
}
