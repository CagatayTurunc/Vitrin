using Microsoft.Extensions.Http.Resilience;
using Polly;

namespace Vitrin.Gateway.Resilience;

/// <summary>
/// YARP reverse proxy için Polly resilience pipeline'larını konfigüre eder.
///
/// Her cluster farklı bir SLA ve davranış profiline sahiptir:
/// - Auth / Product / Comment → kritik yollar, daha agresif circuit breaker
/// - Voting                   → yüksek yazma hacmi, kısa timeout, az retry
/// - Analytics / Notification / AI → arka plan, yüksek tolerans, uzun timeout
///
/// Polly v8 pipeline sırası (içten dışa):
///   Timeout → Retry → Circuit Breaker
/// Yani: önce bireysel istek timeout'u, sonra retry, en dışta circuit breaker.
/// </summary>
public static class VitrinResilienceExtensions
{
    /// <summary>
    /// Named HttpClient'ları Polly resilience pipeline'ları ile kayıt eder.
    /// YARP'ın IForwarderHttpClientFactory'si bu client'ları cluster ID'ye göre seçer.
    /// </summary>
    public static IServiceCollection AddVitrinResiliencePolicies(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var section = configuration.GetSection("Resilience");

        // ── Kritik cluster'lar (Auth, Product, Comment) ──────────────────────
        services
            .AddHttpClient(ResilienceClientNames.Critical)
            .AddResilienceHandler(ResilienceClientNames.Critical, pipeline =>
            {
                var opts = section.GetSection("Critical").Get<ResilienceOptions>()
                           ?? ResilienceOptions.Critical;
                ConfigurePipeline(pipeline, opts, "critical");
            });

        // ── Voting cluster ────────────────────────────────────────────────────
        services
            .AddHttpClient(ResilienceClientNames.Voting)
            .AddResilienceHandler(ResilienceClientNames.Voting, pipeline =>
            {
                var opts = section.GetSection("Voting").Get<ResilienceOptions>()
                           ?? ResilienceOptions.Voting;
                ConfigurePipeline(pipeline, opts, "voting");
            });

        // ── Tolerant cluster'lar (Analytics, Notification, AI) ───────────────
        services
            .AddHttpClient(ResilienceClientNames.Tolerant)
            .AddResilienceHandler(ResilienceClientNames.Tolerant, pipeline =>
            {
                var opts = section.GetSection("Tolerant").Get<ResilienceOptions>()
                           ?? ResilienceOptions.Tolerant;
                ConfigurePipeline(pipeline, opts, "tolerant");
            });

        return services;
    }

    private static void ConfigurePipeline(
        ResiliencePipelineBuilder<HttpResponseMessage> pipeline,
        ResilienceOptions opts,
        string profileName)
    {
        pipeline
            // 1. Timeout — bireysel istek timeout'u (retry başına uygulanır)
            .AddTimeout(TimeSpan.FromSeconds(opts.TimeoutSeconds))

            // 2. Retry — sadece geçici/ağ hatalarında; 4xx'e retry yok
            .AddRetry(new HttpRetryStrategyOptions
            {
                MaxRetryAttempts = opts.RetryCount,
                Delay            = TimeSpan.FromMilliseconds(opts.RetryBaseDelayMs),
                BackoffType      = DelayBackoffType.Exponential,
                UseJitter        = true,
                ShouldHandle     = static args =>
                    ValueTask.FromResult(
                        HttpClientResiliencePredicates.IsTransient(args.Outcome))
            })

            // 3. Circuit Breaker — en dışta; eşik aşılınca tüm istekleri keser
            .AddCircuitBreaker(new HttpCircuitBreakerStrategyOptions
            {
                SamplingDuration  = TimeSpan.FromSeconds(opts.CbSamplingSeconds),
                MinimumThroughput = opts.CbMinimumThroughput,
                FailureRatio      = opts.CbFailureRatio,
                BreakDuration     = TimeSpan.FromSeconds(opts.CbBreakSeconds),
                OnOpened = args =>
                {
                    // Circuit breaker açıldığında log yaz — servis sorununu gösterir
                    // ILogger'a DI üzerinden ulaşmak yerine loglama Prometheus metric ile yapılır;
                    // basit tutmak için burada bir arka plan log'u kullanmıyoruz.
                    // Grafana'daki circuit_breaker_state metric'i bu durumu gösterir.
                    Console.Error.WriteLine(
                        $"[CircuitBreaker] OPENED — profile: {profileName}, " +
                        $"duration: {args.BreakDuration.TotalSeconds}s");
                    return ValueTask.CompletedTask;
                },
                OnClosed = _ =>
                {
                    Console.Error.WriteLine(
                        $"[CircuitBreaker] CLOSED — profile: {profileName}, service recovering");
                    return ValueTask.CompletedTask;
                },
                OnHalfOpened = _ =>
                {
                    Console.Error.WriteLine(
                        $"[CircuitBreaker] HALF-OPEN — profile: {profileName}, probing...");
                    return ValueTask.CompletedTask;
                }
            });
    }
}

/// <summary>
/// Named HttpClient isimleri — bir yerde sabit string yerine buradan referans al.
/// </summary>
public static class ResilienceClientNames
{
    public const string Critical = "Vitrin.Critical";
    public const string Voting   = "Vitrin.Voting";
    public const string Tolerant = "Vitrin.Tolerant";
}

/// <summary>
/// appsettings "Resilience:{Critical|Voting|Tolerant}" bölümüne karşılık gelen POCO.
/// Tüm değerler config'den okunur; yoksa baked-in static default'lar devreye girer.
/// </summary>
public sealed class ResilienceOptions
{
    // ── Per-request timeout ──────────────────────────────────────────────────
    public double TimeoutSeconds { get; set; } = 10;

    // ── Retry ────────────────────────────────────────────────────────────────
    public int    RetryCount       { get; set; } = 3;
    public double RetryBaseDelayMs { get; set; } = 200;

    // ── Circuit Breaker ──────────────────────────────────────────────────────
    public double CbSamplingSeconds   { get; set; } = 30;
    public int    CbMinimumThroughput { get; set; } = 5;
    public double CbFailureRatio      { get; set; } = 0.5;
    public double CbBreakSeconds      { get; set; } = 30;

    // ── Baked-in default profiller ───────────────────────────────────────────
    /// <summary>Auth, Product, Comment — kullanıcının doğrudan hissedeceği yollar.</summary>
    public static readonly ResilienceOptions Critical = new()
    {
        TimeoutSeconds      = 8,
        RetryCount          = 3,
        RetryBaseDelayMs    = 150,
        CbSamplingSeconds   = 30,
        CbMinimumThroughput = 5,
        CbFailureRatio      = 0.5,
        CbBreakSeconds      = 30
    };

    /// <summary>Voting — yüksek yazma hacmi, idempotency nedeniyle az retry.</summary>
    public static readonly ResilienceOptions Voting = new()
    {
        TimeoutSeconds      = 5,
        RetryCount          = 1,
        RetryBaseDelayMs    = 100,
        CbSamplingSeconds   = 20,
        CbMinimumThroughput = 10,
        CbFailureRatio      = 0.6,
        CbBreakSeconds      = 20
    };

    /// <summary>Analytics, Notification, AI — arka plan; yüksek tolerans, uzun timeout.</summary>
    public static readonly ResilienceOptions Tolerant = new()
    {
        TimeoutSeconds      = 15,
        RetryCount          = 2,
        RetryBaseDelayMs    = 300,
        CbSamplingSeconds   = 60,
        CbMinimumThroughput = 3,
        CbFailureRatio      = 0.7,
        CbBreakSeconds      = 60
    };
}
