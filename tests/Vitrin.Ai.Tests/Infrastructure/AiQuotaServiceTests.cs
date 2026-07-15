using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Vitrin.Ai.Infrastructure.Data;
using Vitrin.Ai.Infrastructure.Services;
using Xunit;

namespace Vitrin.Ai.Tests.Infrastructure;

public sealed class AiQuotaServiceTests
{
    [Fact]
    public async Task TryConsume_persists_usage_and_denies_requests_over_the_limit()
    {
        var now = new DateTimeOffset(2026, 7, 14, 12, 0, 0, TimeSpan.Zero);
        await using var fixture = await QuotaFixture.CreateAsync(now, dailyLimit: 2);
        var userId = Guid.NewGuid();

        var first = await fixture.Service.TryConsumeAsync(userId, CancellationToken.None);
        var second = await fixture.Service.TryConsumeAsync(userId, CancellationToken.None);
        var third = await fixture.Service.TryConsumeAsync(userId, CancellationToken.None);

        first.IsAllowed.Should().BeTrue();
        first.RemainingRequests.Should().Be(1);
        second.IsAllowed.Should().BeTrue();
        second.RemainingRequests.Should().Be(0);
        third.IsAllowed.Should().BeFalse();
        (await fixture.DbContext.AiUsageQuotas.SingleAsync()).RequestCount.Should().Be(2);
    }

    [Fact]
    public async Task TryConsume_starts_a_new_quota_period_on_the_next_utc_day()
    {
        var timeProvider = new MutableTimeProvider(
            new DateTimeOffset(2026, 7, 14, 23, 59, 0, TimeSpan.Zero));
        await using var fixture = await QuotaFixture.CreateAsync(timeProvider, dailyLimit: 1);
        var userId = Guid.NewGuid();

        var first = await fixture.Service.TryConsumeAsync(userId, CancellationToken.None);
        timeProvider.UtcNow = new DateTimeOffset(2026, 7, 15, 0, 1, 0, TimeSpan.Zero);
        var nextDay = await fixture.Service.TryConsumeAsync(userId, CancellationToken.None);

        first.IsAllowed.Should().BeTrue();
        nextDay.IsAllowed.Should().BeTrue();
        (await fixture.DbContext.AiUsageQuotas.CountAsync()).Should().Be(2);
    }

    private sealed class QuotaFixture : IAsyncDisposable
    {
        private readonly SqliteConnection _connection;

        private QuotaFixture(
            SqliteConnection connection,
            AiDbContext dbContext,
            AiQuotaService service)
        {
            _connection = connection;
            DbContext = dbContext;
            Service = service;
        }

        public AiDbContext DbContext { get; }
        public AiQuotaService Service { get; }

        public static Task<QuotaFixture> CreateAsync(DateTimeOffset now, int dailyLimit) =>
            CreateAsync(new MutableTimeProvider(now), dailyLimit);

        public static async Task<QuotaFixture> CreateAsync(
            MutableTimeProvider timeProvider,
            int dailyLimit)
        {
            var connection = new SqliteConnection("Data Source=:memory:");
            await connection.OpenAsync();

            var dbOptions = new DbContextOptionsBuilder<AiDbContext>()
                .UseSqlite(connection)
                .Options;
            var dbContext = new AiDbContext(dbOptions);
            await dbContext.Database.EnsureCreatedAsync();

            var service = new AiQuotaService(
                dbContext,
                Options.Create(new AiQuotaOptions { DailyRequestLimit = dailyLimit }),
                timeProvider);

            return new QuotaFixture(connection, dbContext, service);
        }

        public async ValueTask DisposeAsync()
        {
            await DbContext.DisposeAsync();
            await _connection.DisposeAsync();
        }
    }

    private sealed class MutableTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public DateTimeOffset UtcNow { get; set; } = utcNow;

        public override DateTimeOffset GetUtcNow() => UtcNow;
    }
}
