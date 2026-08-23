using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;

namespace Vitrin.Shared.Infrastructure.Api;

/// <summary>
/// Health check endpoint'lerini güvenli şekilde map eder.
///
/// /health       → Dışarıya açık. Sadece {"status":"healthy"} döner.
///                  DB adı, bağlantı string'i, servis URL'si sızdırmaz.
///
/// /health/detail → Sadece Docker iç ağından erişilebilir (nginx 403 bloklar).
///                  Hangi servisin sağlıksız olduğunu detaylı gösterir.
/// </summary>
public static class VitrinHealthCheckEndpoints
{
    public static WebApplication UseVitrinHealthChecks(this WebApplication app)
    {
        // Dışarıya açık — sadece basit OK/FAIL
        app.MapHealthChecks("/health", new HealthCheckOptions
        {
            ResponseWriter = async (context, report) =>
            {
                context.Response.ContentType = "application/json";
                var status = report.Status == HealthStatus.Healthy ? "healthy" : "unhealthy";
                await context.Response.WriteAsync($"{{\"status\":\"{status}\"}}");
            },
            ResultStatusCodes =
            {
                [HealthStatus.Healthy]   = StatusCodes.Status200OK,
                [HealthStatus.Degraded]  = StatusCodes.Status200OK,
                [HealthStatus.Unhealthy] = StatusCodes.Status503ServiceUnavailable
            }
        });

        // İç ağa açık — detaylı sonuç; nginx bu path'i dışarıya 403 ile bloklar
        app.MapHealthChecks("/health/detail", new HealthCheckOptions
        {
            ResponseWriter = async (context, report) =>
            {
                context.Response.ContentType = "application/json";

                // IsDevelopment kontrolü IHostEnvironment üzerinden yapılır
                var env = context.RequestServices.GetRequiredService<IHostEnvironment>();
                var isDev = env.IsDevelopment();

                var entries = report.Entries.Select(entry => new
                {
                    name        = entry.Key,
                    status      = entry.Value.Status.ToString().ToLowerInvariant(),
                    description = entry.Value.Description,
                    // Exception detayı sadece dev'de; production'da "check failed" yazar
                    error       = isDev
                        ? entry.Value.Exception?.Message
                        : entry.Value.Exception is not null ? "check failed" : null,
                    durationMs  = entry.Value.Duration.TotalMilliseconds
                });

                var result = new
                {
                    status            = report.Status.ToString().ToLowerInvariant(),
                    totalDurationMs   = report.TotalDuration.TotalMilliseconds,
                    entries
                };

                await context.Response.WriteAsJsonAsync(result);
            }
        });

        return app;
    }
}
