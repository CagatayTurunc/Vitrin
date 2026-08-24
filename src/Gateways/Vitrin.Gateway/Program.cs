using StackExchange.Redis;
using Vitrin.Gateway.Resilience;
using Vitrin.Shared.Infrastructure.Auth;
using Vitrin.Shared.Infrastructure.Api;
using Yarp.ReverseProxy.Forwarder;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddVitrinJwtAuthentication(builder.Configuration);
builder.Services.AddHealthChecks();
builder.Services.AddVitrinApiErrors();
builder.Services.AddVitrinRateLimiting();

// Madde 4 — Çıkışta oturumu düşür: Redis token blacklist
// Logout olan kullanıcıların token'ları burada kontrol edilir
var redisConnection = builder.Configuration.GetConnectionString("Redis") ?? "localhost:6379";
builder.Services.AddSingleton<IConnectionMultiplexer>(
    ConnectionMultiplexer.Connect(redisConnection));
builder.Services.AddVitrinTokenBlacklist();

// ── Resilience: Circuit Breaker + Retry + Timeout ──────────────────────────
// Named HttpClient'lar Polly pipeline'ı ile kayıt edilir.
// ResilienceForwarderHttpClientFactory, YARP'ın cluster ID'sine göre
// doğru named client'ı seçerek her servis grubuna farklı policy uygular:
//   auth/product/comment → Critical  (8s timeout, 3 retry, CB %50)
//   voting               → Voting    (5s timeout, 1 retry, CB %60)
//   analytics/notif/ai   → Tolerant  (15s timeout, 2 retry, CB %70)
builder.Services.AddVitrinResiliencePolicies(builder.Configuration);

// YARP konfigürasyonunu appsettings.json dosyasındaki "ReverseProxy" bölümünden alıyoruz.
builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

// YARP'ın default HttpClient factory'sini resilience-aware factory ile override et.
// Artık her YARP proxy isteği Polly pipeline'ından geçer.
builder.Services.AddSingleton<IForwarderHttpClientFactory, ResilienceForwarderHttpClientFactory>();

// CORS Policy for Next.js Frontend
// Allowed origins appsettings "Cors:AllowedOrigins" dizisinden okunur.
// Boş/tanımsız ise varsayılan localhost değerleri kullanılır.
var configuredOrigins = builder.Configuration
    .GetSection("Cors:AllowedOrigins")
    .Get<string[]>();

var allowedOrigins = (configuredOrigins != null && configuredOrigins.Length > 0)
    ? configuredOrigins
    : new[]
    {
        "http://localhost:3000",    // local dev (docker)
        "http://localhost:3001",    // local dev (native)
        "http://localhost:3002",    // local dev (fallback)
        "http://vitrin-web:3000"    // docker iç ağ
    };

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins(allowedOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

var app = builder.Build();

app.UseVitrinApiErrors();
app.UseCors("AllowFrontend");

app.UseAuthentication();
app.Use(async (context, next) =>
{
    // Madde 4 — Çıkışta oturumu düşür: token blacklist kontrolü
    // Login olmuş bir kullanıcı logout sonrasında token'ını kullanamaz
    if (context.User.Identity?.IsAuthenticated == true)
    {
        var jti = context.User.FindFirst(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Jti)?.Value;
        if (!string.IsNullOrWhiteSpace(jti))
        {
            var blacklist = context.RequestServices.GetRequiredService<IJwtTokenBlacklist>();
            if (await blacklist.IsBlacklistedAsync(jti, context.RequestAborted))
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                await context.Response.WriteAsJsonAsync(new
                {
                    title = "Token revoked",
                    detail = "Bu token geçersiz kılınmış. Lütfen yeniden giriş yapın.",
                    code = "auth.token_revoked"
                });
                return;
            }
        }
    }

    var isBanned = string.Equals(
        context.User.FindFirst("vitrin:banned")?.Value,
        "true",
        StringComparison.OrdinalIgnoreCase);
    var path = context.Request.Path;
    var isAppealPath = path.StartsWithSegments("/api/auth/moderation/appeals");
    var isAccountStatusPath = path.StartsWithSegments("/api/auth/users/me");
    var isNotificationPath = path.StartsWithSegments("/api/notifications/me");

    if (isBanned && !isAppealPath && !isAccountStatusPath && !isNotificationPath)
    {
        context.Response.StatusCode = StatusCodes.Status403Forbidden;
        await context.Response.WriteAsJsonAsync(new
        {
            title = "Account suspended",
            detail = "This account is suspended. You can still view your account status and submit an appeal.",
            code = "account.suspended"
        });
        return;
    }

    await next();
});
app.UseRateLimiter();
app.UseAuthorization();

// Dışarıya sadece {"status":"healthy"} döner — DB bağlantı string'i veya servis URL'si sızdırmaz.
// /health/detail endpoint'i sadece iç ağdan erişilebilir (nginx 403 döndürür).
app.UseVitrinHealthChecks();
app.MapGet("/", () => "Vitrin API Gateway is running! (YARP)");

// Gelen istekleri ilgili mikroservislere yönlendirecek olan YARP Middleware'i
app.MapReverseProxy();

app.Run();
