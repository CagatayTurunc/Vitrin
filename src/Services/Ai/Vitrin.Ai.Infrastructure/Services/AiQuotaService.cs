using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Vitrin.Ai.Application.Services;
using Vitrin.Ai.Infrastructure.Data;

namespace Vitrin.Ai.Infrastructure.Services;

public sealed class AiQuotaService(
    AiDbContext dbContext,
    IOptions<AiQuotaOptions> options,
    TimeProvider timeProvider) : IAiQuotaService
{
    public async Task<AiQuotaDecision> TryConsumeAsync(
        Guid userId,
        CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow();
        var periodStartUtc = now.UtcDateTime.Date;
        var resetAtUtc = new DateTimeOffset(periodStartUtc.AddDays(1), TimeSpan.Zero);
        var dailyLimit = options.Value.DailyRequestLimit;

        // A single SQLite UPSERT makes quota consumption atomic. Concurrent requests
        // cannot both observe the same count and increment it beyond the limit.
        var affectedRows = await dbContext.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO AiUsageQuotas (Id, UserId, PeriodStartUtc, RequestCount, LastRequestedAtUtc)
            VALUES ({Guid.NewGuid()}, {userId}, {periodStartUtc}, 1, {now.UtcDateTime})
            ON CONFLICT(UserId, PeriodStartUtc) DO UPDATE SET
                RequestCount = RequestCount + 1,
                LastRequestedAtUtc = excluded.LastRequestedAtUtc
            WHERE RequestCount < {dailyLimit};
            """, cancellationToken);

        if (affectedRows == 0)
        {
            return new AiQuotaDecision(false, 0, resetAtUtc);
        }

        var requestCount = await dbContext.AiUsageQuotas
            .AsNoTracking()
            .Where(item => item.UserId == userId && item.PeriodStartUtc == periodStartUtc)
            .Select(item => item.RequestCount)
            .SingleAsync(cancellationToken);

        return new AiQuotaDecision(
            true,
            Math.Max(0, dailyLimit - requestCount),
            resetAtUtc);
    }
}
