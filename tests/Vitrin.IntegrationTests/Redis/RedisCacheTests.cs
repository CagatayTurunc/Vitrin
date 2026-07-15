using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using StackExchange.Redis;
using Vitrin.IntegrationTests.Infrastructure;
using Vitrin.Shared.Infrastructure.Redis;

namespace Vitrin.IntegrationTests.Redis;

[Collection(RedisCollection.Name)]
[Trait("Category", "Integration")]
public sealed class RedisCacheTests(RedisFixture fixture)
{
    [Fact]
    public async Task Cache_round_trip_preserves_payload_and_expiration()
    {
        await using var redis = await ConnectionMultiplexer.ConnectAsync(fixture.ConnectionString);
        var cache = new RedisCacheService(redis, NullLogger<RedisCacheService>.Instance);
        var key = $"integration:cache:{Guid.NewGuid():N}";
        var value = new CacheSample(Guid.NewGuid(), "testcontainers");

        await cache.SetAsync(key, value, TimeSpan.FromMinutes(2));
        var cached = await cache.GetAsync<CacheSample>(key);
        var ttl = await redis.GetDatabase().KeyTimeToLiveAsync(key);

        cached.Should().Be(value);
        ttl.Should().NotBeNull().And.BeGreaterThan(TimeSpan.FromMinutes(1));
    }

    [Fact]
    public async Task Exact_and_pattern_invalidation_remove_only_matching_keys()
    {
        await using var redis = await ConnectionMultiplexer.ConnectAsync(fixture.ConnectionString);
        var cache = new RedisCacheService(redis, NullLogger<RedisCacheService>.Instance);
        var prefix = $"integration:invalidate:{Guid.NewGuid():N}";
        var first = $"{prefix}:first";
        var second = $"{prefix}:second";
        var unrelated = $"integration:keep:{Guid.NewGuid():N}";

        await cache.SetAsync(first, 1, TimeSpan.FromMinutes(2));
        await cache.SetAsync(second, 2, TimeSpan.FromMinutes(2));
        await cache.SetAsync(unrelated, 3, TimeSpan.FromMinutes(2));

        await cache.InvalidateAsync(first);
        (await cache.GetAsync<int?>(first)).Should().BeNull();

        await cache.InvalidatePatternAsync($"{prefix}:*");
        (await cache.GetAsync<int?>(second)).Should().BeNull();
        (await cache.GetAsync<int>(unrelated)).Should().Be(3);
    }

    private sealed record CacheSample(Guid Id, string Source);
}
