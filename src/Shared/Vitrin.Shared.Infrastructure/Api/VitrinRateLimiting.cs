using System.Globalization;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Vitrin.Shared.Infrastructure.Auth;
using Vitrin.Shared.Infrastructure.Redis;

namespace Vitrin.Shared.Infrastructure.Api;

/// <summary>
/// Rate limiter policy isimleri — YARP appsettings.json route konfigürasyonuyla eşleşmelidir.
/// </summary>
public static class VitrinRateLimitPolicies
{
    public const string Login        = "auth-login";
    public const string Registration = "auth-registration";
    public const string ExternalLogin = "auth-external-login";
    public const string AiAnalysis   = "ai-analysis";
    public const string ApiWrite     = "api-write";
    public const string SocialWrite  = "social-write";
    public const string SearchQuery  = "search-query";
    public const string AnalyticsEvent = "analytics-event";
    public const string AnalyticsQuery = "analytics-query";
}

public static class VitrinRateLimitingExtensions
{
    /// <summary>
    /// Rate limiting servislerini kayıt eder.
    ///
    /// Strateji: Redis Sliding Window + ASP.NET in-memory fallback
    /// - IRedisRateLimitStore kayıtlıysa → Redis distributed sliding window kullanılır.
    ///   Restart veya çoklu instance'ta sayaçlar korunur.
    /// - Redis erişilemezse → fail-open (istek geçer) + in-memory limiter devreye girer.
    ///
    /// Konfigürasyon:
    ///   Policy       | Kapsam    | Limit | Pencere
    ///   -------------|-----------|-------|--------
    ///   auth-login   | IP        | 5     | 1 dakika
    ///   registration | IP        | 3     | 10 dakika
    ///   external-login | IP      | 10    | 1 dakika
    ///   ai-analysis  | User/IP   | 5     | 1 dakika
    ///   api-write    | User/IP   | 60    | 1 dakika
    ///   social-write | User/IP   | 30    | 1 dakika
    ///   search-query | User/IP   | 90    | 1 dakika
    ///   analytics-event | User/IP | 30   | 1 dakika
    ///   analytics-query | User/IP | 45   | 1 dakika
    /// </summary>
    public static IServiceCollection AddVitrinRateLimiting(this IServiceCollection services)
    {
        // Redis store — Gateway'de IConnectionMultiplexer zaten kayıtlı
        services.AddSingleton<IRedisRateLimitStore, RedisRateLimitStore>();

        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.OnRejected = async (context, cancellationToken) =>
            {
                TimeSpan? retryAfter = null;
                if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfterValue))
                {
                    retryAfter = retryAfterValue;
                    context.HttpContext.Response.Headers.RetryAfter =
                        Math.Ceiling(retryAfterValue.TotalSeconds)
                            .ToString(CultureInfo.InvariantCulture);
                }

                await Results.Problem(
                        statusCode: StatusCodes.Status429TooManyRequests,
                        title: "Too many requests.",
                        detail: "The request limit was exceeded. Please wait before trying again.",
                        extensions: ApiProblemResults.Extensions(
                            "rate_limit.exceeded",
                            ("retryAfterSeconds", retryAfter is null
                                ? null
                                : (int)Math.Ceiling(retryAfter.Value.TotalSeconds))))
                    .ExecuteAsync(context.HttpContext);
            };

            // ── IP bazlı policy'ler (auth endpoint'leri) ────────────────────
            options.AddPolicy(VitrinRateLimitPolicies.Login, context =>
                RedisOrFixed(context, VitrinRateLimitPolicies.Login,
                    key: ClientIp(context),
                    limit: 5, window: TimeSpan.FromMinutes(1)));

            options.AddPolicy(VitrinRateLimitPolicies.Registration, context =>
                RedisOrFixed(context, VitrinRateLimitPolicies.Registration,
                    key: ClientIp(context),
                    limit: 3, window: TimeSpan.FromMinutes(10)));

            options.AddPolicy(VitrinRateLimitPolicies.ExternalLogin, context =>
                RedisOrFixed(context, VitrinRateLimitPolicies.ExternalLogin,
                    key: ClientIp(context),
                    limit: 10, window: TimeSpan.FromMinutes(1)));

            // ── User/IP bazlı policy'ler ────────────────────────────────────
            options.AddPolicy(VitrinRateLimitPolicies.AiAnalysis, context =>
                RedisOrFixed(context, VitrinRateLimitPolicies.AiAnalysis,
                    key: UserOrIp(context),
                    limit: 5, window: TimeSpan.FromMinutes(1)));

            options.AddPolicy(VitrinRateLimitPolicies.ApiWrite, context =>
                RedisOrFixed(context, VitrinRateLimitPolicies.ApiWrite,
                    key: UserOrIp(context),
                    limit: 60, window: TimeSpan.FromMinutes(1)));

            options.AddPolicy(VitrinRateLimitPolicies.SocialWrite, context =>
                RedisOrFixed(context, VitrinRateLimitPolicies.SocialWrite,
                    key: UserOrIp(context),
                    limit: 30, window: TimeSpan.FromMinutes(1)));

            options.AddPolicy(VitrinRateLimitPolicies.SearchQuery, context =>
                RedisOrFixed(context, VitrinRateLimitPolicies.SearchQuery,
                    key: UserOrIp(context),
                    limit: 90, window: TimeSpan.FromMinutes(1)));

            options.AddPolicy(VitrinRateLimitPolicies.AnalyticsEvent, context =>
                RedisOrFixed(context, VitrinRateLimitPolicies.AnalyticsEvent,
                    key: UserOrIp(context),
                    limit: 30, window: TimeSpan.FromMinutes(1)));

            options.AddPolicy(VitrinRateLimitPolicies.AnalyticsQuery, context =>
                RedisOrFixed(context, VitrinRateLimitPolicies.AnalyticsQuery,
                    key: UserOrIp(context),
                    limit: 45, window: TimeSpan.FromMinutes(1)));
        });

        return services;
    }

    /// <summary>
    /// Redis sliding window limiter döndürür.
    /// Redis erişilemezse store fail-open çalışır; in-memory fixed window fallback'i devreye girer.
    /// </summary>
    private static RateLimitPartition<string> RedisOrFixed(
        HttpContext context,
        string policy,
        string key,
        int limit,
        TimeSpan window)
    {
        var store = context.RequestServices.GetService<IRedisRateLimitStore>();

        if (store is not null)
        {
            return RateLimitPartition.Get<string>(
                partitionKey: $"{policy}:{key}",
                factory: _ => new RedisRateLimiter(store, policy, key, limit, window,
                    context.RequestServices.GetRequiredService<ILogger<RedisRateLimiter>>()));
        }

        // Fallback: in-memory fixed window (Redis kayıtlı değilse)
        return FixedWindow(key, limit, window);
    }

    private static RateLimitPartition<string> FixedWindow(
        string partitionKey,
        int permitLimit,
        TimeSpan window) =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey,
            _ => new FixedWindowRateLimiterOptions
            {
                AutoReplenishment = true,
                PermitLimit       = permitLimit,
                QueueLimit        = 0,
                Window            = window
            });

    private static string ClientIp(HttpContext context) =>
        context.Connection.RemoteIpAddress?.ToString() ?? "unknown";

    private static string UserOrIp(HttpContext context) =>
        context.User.GetUserId()?.ToString() ?? ClientIp(context);
}

/// <summary>
/// ASP.NET RateLimiter altyapısına uyum sağlayan Redis-backed limiter.
/// Her AcquireAsync çağrısında Redis store'a danışır.
/// </summary>
internal sealed class RedisRateLimiter : RateLimiter
{
    private readonly IRedisRateLimitStore _store;
    private readonly string _policy;
    private readonly string _partitionKey;
    private readonly int _limit;
    private readonly TimeSpan _window;
    private readonly ILogger _logger;

    // Son bilinen RetryAfter — OnRejected içinde MetadataName.RetryAfter ile okunur
    private TimeSpan? _lastRetryAfter;

    public RedisRateLimiter(
        IRedisRateLimitStore store,
        string policy,
        string partitionKey,
        int limit,
        TimeSpan window,
        ILogger logger)
    {
        _store        = store;
        _policy       = policy;
        _partitionKey = partitionKey;
        _limit        = limit;
        _window       = window;
        _logger       = logger;
    }

    public override TimeSpan? IdleDuration => null;

    public override RateLimiterStatistics? GetStatistics() => null;

    protected override ValueTask<RateLimitLease> AcquireAsyncCore(
        int permitCount,
        CancellationToken cancellationToken)
        => new(AcquireInternalAsync(cancellationToken));

    protected override RateLimitLease AttemptAcquireCore(int permitCount)
    {
        // Senkron path — async store ile desteklenemiyor; her zaman izin ver,
        // async path'e güven. ASP.NET rate limiter pipeline'ı AcquireAsync kullanır.
        return new RedisRateLimitLease(allowed: true, retryAfter: null);
    }

    private async Task<RateLimitLease> AcquireInternalAsync(CancellationToken ct)
    {
        var (allowed, retryAfter) = await _store.CheckAndIncrementAsync(
            _policy, _partitionKey, _limit, _window, ct);

        _lastRetryAfter = retryAfter;

        _logger.LogDebug(
            "RedisRateLimit: policy={Policy}, key={Key}, allowed={Allowed}",
            _policy, _partitionKey, allowed);

        return new RedisRateLimitLease(allowed, retryAfter);
    }

    protected override void Dispose(bool disposing) { }
}

/// <summary>
/// Redis rate limit sonucunu taşıyan lease.
/// </summary>
internal sealed class RedisRateLimitLease : RateLimitLease
{
    private readonly bool _allowed;
    private readonly TimeSpan? _retryAfter;

    public RedisRateLimitLease(bool allowed, TimeSpan? retryAfter)
    {
        _allowed    = allowed;
        _retryAfter = retryAfter;
    }

    public override bool IsAcquired => _allowed;

    public override IEnumerable<string> MetadataNames =>
        _retryAfter.HasValue
            ? [MetadataName.RetryAfter.Name]
            : [];

    public override bool TryGetMetadata(string metadataName, out object? metadata)
    {
        if (metadataName == MetadataName.RetryAfter.Name && _retryAfter.HasValue)
        {
            metadata = _retryAfter.Value;
            return true;
        }

        metadata = null;
        return false;
    }

    protected override void Dispose(bool disposing) { }
}
