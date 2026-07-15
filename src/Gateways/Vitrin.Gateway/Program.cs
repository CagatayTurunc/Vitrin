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
                "http://localhost:3000",    // local dev
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
app.UseRateLimiter();
app.UseAuthorization();

app.MapHealthChecks("/health");
app.MapGet("/", () => "Vitrin API Gateway is running! (YARP)");

// Gelen istekleri ilgili mikroservislere yönlendirecek olan YARP Middleware'i
app.MapReverseProxy();

app.Run();
