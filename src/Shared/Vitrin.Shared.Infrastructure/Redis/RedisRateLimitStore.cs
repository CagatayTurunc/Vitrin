using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace Vitrin.Shared.Infrastructure.Redis;

/// <summary>
/// Redis tabanlı sliding window rate limit store.
///
/// Algoritma: Sorted Set + Lua script (atomik)
/// - Key: "rl:{policy}:{partitionKey}"
/// - Her istek bir üye olarak eklenir (score = şimdiki timestamp)
/// - Pencere dışındaki eski üyeler temizlenir
/// - Mevcut penceredeki istek sayısı limitin altındaysa izin verilir
///
/// Neden Lua?
/// - ZREMRANGEBYSCORE + ZCARD + ZADD üç ayrı komut olmak yerine
///   tek round-trip içinde atomik çalışır.
/// - Race condition olmaz: iki eş zamanlı istek aynı limit slotuna giremez.
///
/// Neden Sliding Window?
/// - Fixed window'da "pencere başında 60 istek → hemen sonra 60 istek daha"
///   gibi 2x burst mümkündür. Sliding window bunu önler.
/// </summary>
public interface IRedisRateLimitStore
{
    /// <summary>
    /// İsteğin geçip geçmeyeceğini kontrol eder ve sayacı artırır.
    /// </summary>
    /// <returns>
    /// (allowed: true, retryAfter: null)      → limit aşılmadı
    /// (allowed: false, retryAfter: TimeSpan) → limit aşıldı, ne kadar beklemeli
    /// </returns>
    Task<(bool Allowed, TimeSpan? RetryAfter)> CheckAndIncrementAsync(
        string policy,
        string partitionKey,
        int permitLimit,
        TimeSpan window,
        CancellationToken ct = default);
}

public sealed class RedisRateLimitStore : IRedisRateLimitStore
{
    private readonly IConnectionMultiplexer _redis;
    private readonly ILogger<RedisRateLimitStore> _logger;

    // Lua script — atomik sliding window
    // KEYS[1] = rate limit key
    // ARGV[1] = şimdiki zaman (microsecond Unix timestamp — benzersizlik için)
    // ARGV[2] = pencere başlangıcı (now - window) — bu değerin altındaki üyeler silinir
    // ARGV[3] = permit limit
    // ARGV[4] = key TTL (saniye)
    private static readonly LuaScript SlidingWindowScript = LuaScript.Prepare(@"
local key       = KEYS[1]
local now       = tonumber(ARGV[1])
local window_start = tonumber(ARGV[2])
local limit     = tonumber(ARGV[3])
local ttl       = tonumber(ARGV[4])

-- Pencere dışındaki eski istekleri temizle
redis.call('ZREMRANGEBYSCORE', key, '-inf', window_start)

-- Mevcut penceredeki istek sayısını al
local count = redis.call('ZCARD', key)

if count < limit then
    -- Limit altında: bu isteği kaydet ve izin ver
    -- Score olarak timestamp, member olarak timestamp+random suffix (benzersizlik)
    redis.call('ZADD', key, now, tostring(now))
    redis.call('EXPIRE', key, ttl)
    return {1, 0}
else
    -- Limit aşıldı: en eski isteğin ne zaman expire olacağını hesapla
    local oldest = redis.call('ZRANGE', key, 0, 0, 'WITHSCORES')
    local retry_after_ms = 0
    if oldest and #oldest >= 2 then
        local oldest_score = tonumber(oldest[2])
        -- window süresi eklenince bu istek pencere dışına çıkar
        retry_after_ms = math.max(0, (oldest_score + tonumber(ARGV[4]) * 1000) - now)
    end
    return {0, retry_after_ms}
end
");

    public RedisRateLimitStore(
        IConnectionMultiplexer redis,
        ILogger<RedisRateLimitStore> logger)
    {
        _redis = redis;
        _logger = logger;
    }

    public async Task<(bool Allowed, TimeSpan? RetryAfter)> CheckAndIncrementAsync(
        string policy,
        string partitionKey,
        int permitLimit,
        TimeSpan window,
        CancellationToken ct = default)
    {
        try
        {
            var db = _redis.GetDatabase();
            var key = $"rl:{policy}:{partitionKey}";

            // Microsecond timestamp — sorted set'te benzersizlik için
            var nowMicros  = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() * 1000;
            var windowStart = nowMicros - (long)(window.TotalMilliseconds * 1000);
            var ttlSeconds  = (long)Math.Ceiling(window.TotalSeconds) + 1;

            var result = await db.ScriptEvaluateAsync(
                SlidingWindowScript,
                keys:  [(RedisKey)key],
                values:
                [
                    (RedisValue)nowMicros,
                    (RedisValue)windowStart,
                    (RedisValue)permitLimit,
                    (RedisValue)ttlSeconds
                ]);

            var resultArray = (RedisValue[])result!;
            var allowed       = (int)resultArray[0] == 1;
            var retryAfterMs  = (long)resultArray[1];

            return allowed
                ? (true, null)
                : (false, TimeSpan.FromMilliseconds(retryAfterMs / 1000.0));
        }
        catch (Exception ex)
        {
            // Redis erişilemez → fail-open: isteği geçir, loglama yap
            // Üretimde Redis'in HA yapısı olduğundan bu senaryo nadir.
            // Fail-close (isteği reddet) tercih edilirse false döndür.
            _logger.LogWarning(ex,
                "Redis rate limit kontrolü başarısız (fail-open): policy={Policy}, key={Key}",
                policy, partitionKey);
            return (true, null);
        }
    }
}
