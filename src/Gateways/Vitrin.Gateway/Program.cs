using Vitrin.Shared.Infrastructure.Auth;
using Vitrin.Shared.Infrastructure.Api;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddVitrinJwtAuthentication(builder.Configuration);
builder.Services.AddHealthChecks();
builder.Services.AddVitrinApiErrors();
builder.Services.AddVitrinRateLimiting();

// YARP konfigürasyonunu appsettings.json dosyasındaki "ReverseProxy" bölümünden alıyoruz.
builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

// CORS Policy for Next.js Frontend
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins(
                "http://localhost:3000",    // local dev (docker)
                "http://localhost:3001",    // local dev (native)
                "http://localhost:3002",    // local dev (fallback)
                "http://vitrin-web:3000"    // docker iç ağ
              )
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

app.MapHealthChecks("/health");
app.MapGet("/", () => "Vitrin API Gateway is running! (YARP)");

// Gelen istekleri ilgili mikroservislere yönlendirecek olan YARP Middleware'i
app.MapReverseProxy();

app.Run();
