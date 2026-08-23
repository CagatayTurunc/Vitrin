using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Vitrin.Shared.Infrastructure.Api;

/// <summary>
/// Health check endpoint'lerini güvenli şekilde map eder.
///
/// Neden iki ayrı endpoint?
///
/// /health → Dışarıya açık. Nginx üzerinden erişilebilir. Sadece {"status":"healthy"} veya
///            {"status":"unhealthy"} döner. DB adı, bağlantı string'i, servis URL'si gibi
///            internal bilgi sızdırmaz. Load balancer ve uptime monitor'lar bunu kullanır.
///
/// /health/detail → Sadece Docker iç ağından erişilebilir (nginx bu path'i bloklar).
///                   Hangi servisin (DB, Redis, Kafka) sağlıksız olduğunu detaylı gösterir.
///                   Prometheus ve alerting sistemi bunu kullanır.
///
/// Neden önemli? /health detaylı bilgi döndürürse saldırgan "Database=vitrin_auth;Host=postgres"
/// gibi iç mimari bilgisini öğrenebilir. Bu OSINT (Open Source Intelligence) için değerli veri.
/// </summary>
public static class VitrinHealthCheckEndpoints
{
    public static IApplicationBuilder UseVitrinHealthChecks(this WebApplication app)
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
            // Tüm check'leri çalıştır ama sadece genel sonucu döndür
            ResultStatusCodes =
            {
                [HealthStatus.Healthy] = StatusCodes.Status200OK,
                [HealthStatus.Degraded] = StatusCodes.Status200OK,  // degraded = hâlâ ayakta
                [HealthStatus.Unhealthy] = StatusCodes.Status503ServiceUnavailable
            }
        });

        // İç ağa açık — detaylı sonuç (Prometheus, alerting)
        // Nginx bu path'i 403 döndürecek şekilde yapılandırılmalı
        app.MapHealthChecks("/health/detail", new HealthCheckOptions
        {
            ResponseWriter = async (context, report) =>
            {
                context.Response.ContentType = "application/json";
                var entries = report.Entries.Select(entry => new
                {
                    name = entry.Key,
                    status = entry.Value.Status.ToString().ToLowerInvariant(),
                    description = entry.Value.Description,
                    // Exception detayı sadece development'ta — production'da gizle
                    error = app.Environment.IsDevelopment()
                        ? entry.Value.Exception?.Message
                        : entry.Value.Exception is not null ? "check failed" : null,
                    duration = entry.Value.Duration.TotalMilliseconds
                });

                var result = new
                {
                    status = report.Status.ToString().ToLowerInvariant(),
                    totalDuration = report.TotalDuration.TotalMilliseconds,
                    entries
                };

                await context.Response.WriteAsJsonAsync(result);
            }
        });

        return app;
    }
}
