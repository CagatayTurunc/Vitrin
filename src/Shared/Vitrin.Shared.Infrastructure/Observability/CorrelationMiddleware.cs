using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace Vitrin.Shared.Infrastructure.Observability;

/// <summary>
/// Request correlation ID ve distributed tracing için middleware
/// </summary>
public class CorrelationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<CorrelationMiddleware> _logger;
    
    public const string CorrelationIdHeaderName = "X-Correlation-ID";
    public const string TraceIdHeaderName = "X-Trace-ID";

    public CorrelationMiddleware(RequestDelegate next, ILogger<CorrelationMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Correlation ID al veya oluştur
        var correlationId = GetOrCreateCorrelationId(context);
        
        // Response header'lara ekle
        context.Response.Headers.TryAdd(CorrelationIdHeaderName, correlationId);
        
        // Trace ID varsa response'a ekle
        var traceId = Activity.Current?.TraceId.ToString();
        if (!string.IsNullOrEmpty(traceId))
        {
            context.Response.Headers.TryAdd(TraceIdHeaderName, traceId);
        }

        // Scope'a correlation ID ekle (Serilog için)
        using var scope = _logger.BeginScope(new Dictionary<string, object>
        {
            ["CorrelationId"] = correlationId,
            ["TraceId"] = traceId ?? "unknown",
            ["RequestPath"] = context.Request.Path,
            ["RequestMethod"] = context.Request.Method,
            ["UserAgent"] = context.Request.Headers.UserAgent.ToString(),
            ["RemoteIpAddress"] = context.Connection.RemoteIpAddress?.ToString() ?? "unknown"
        });

        // Activity'ye correlation ID ekle
        Activity.Current?.SetTag("correlation.id", correlationId);
        Activity.Current?.SetTag("http.method", context.Request.Method);
        Activity.Current?.SetTag("http.route", context.Request.Path);
        Activity.Current?.SetTag("http.scheme", context.Request.Scheme);
        Activity.Current?.SetTag("http.host", context.Request.Host.ToString());
        Activity.Current?.SetTag("user.agent", context.Request.Headers.UserAgent.ToString());

        var stopwatch = Stopwatch.StartNew();
        
        try
        {
            await _next(context);
        }
        finally
        {
            stopwatch.Stop();
            
            // Response bilgilerini activity'ye ekle
            Activity.Current?.SetTag("http.status_code", context.Response.StatusCode);
            Activity.Current?.SetTag("http.response_time_ms", stopwatch.ElapsedMilliseconds);

            // Request logging
            _logger.LogInformation(
                "HTTP {Method} {Path} responded {StatusCode} in {ElapsedMs}ms",
                context.Request.Method,
                context.Request.Path,
                context.Response.StatusCode,
                stopwatch.ElapsedMilliseconds);

            // Slow request uyarısı
            if (stopwatch.ElapsedMilliseconds > 5000) // 5 saniyeden uzun
            {
                _logger.LogWarning(
                    "Slow request detected: {Method} {Path} took {ElapsedMs}ms",
                    context.Request.Method,
                    context.Request.Path,
                    stopwatch.ElapsedMilliseconds);
            }

            // Error status code logging
            if (context.Response.StatusCode >= 400)
            {
                var logLevel = context.Response.StatusCode >= 500 ? LogLevel.Error : LogLevel.Warning;
                _logger.Log(logLevel,
                    "HTTP error response: {Method} {Path} returned {StatusCode}",
                    context.Request.Method,
                    context.Request.Path,
                    context.Response.StatusCode);
            }
        }
    }

    private static string GetOrCreateCorrelationId(HttpContext context)
    {
        // Header'dan correlation ID al
        if (context.Request.Headers.TryGetValue(CorrelationIdHeaderName, out var correlationId) && 
            !string.IsNullOrEmpty(correlationId))
        {
            return correlationId.ToString();
        }

        // Trace ID'yi correlation ID olarak kullan
        var traceId = Activity.Current?.TraceId.ToString();
        if (!string.IsNullOrEmpty(traceId))
        {
            return traceId;
        }

        // Yeni correlation ID oluştur
        return Guid.NewGuid().ToString("N")[..16]; // 16 karakter kısa ID
    }
}

/// <summary>
/// Correlation middleware extension method
/// </summary>
public static class CorrelationMiddlewareExtensions
{
    public static IApplicationBuilder UseVitrinCorrelation(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<CorrelationMiddleware>();
    }
}