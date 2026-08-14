using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace Vitrin.Shared.Infrastructure.Observability;

public static class LoggingExtensions
{
    /// <summary>
    /// İş operasyonları için structured logging desteği
    /// </summary>
    public static void LogBusinessOperation(this ILogger logger, string operation, object? data = null, string? userId = null)
    {
        using var activity = Activity.Current?.Source.StartActivity($"Business.{operation}");
        activity?.SetTag("operation.type", "business");
        activity?.SetTag("user.id", userId);

        logger.LogInformation("Business operation: {Operation} executed by {UserId} with data: {@Data}", 
            operation, userId ?? "anonymous", data);
    }

    /// <summary>
    /// Performans metrikleri için logging
    /// </summary>
    public static void LogPerformanceMetric(this ILogger logger, string metricName, long durationMs, Dictionary<string, object>? tags = null)
    {
        using var activity = Activity.Current?.Source.StartActivity($"Performance.{metricName}");
        activity?.SetTag("metric.type", "performance");
        activity?.SetTag("duration.ms", durationMs);
        
        if (tags != null)
        {
            foreach (var tag in tags)
            {
                activity?.SetTag(tag.Key, tag.Value?.ToString());
            }
        }

        logger.LogInformation("Performance metric: {MetricName} completed in {Duration}ms with tags: {@Tags}", 
            metricName, durationMs, tags);
    }

    /// <summary>
    /// Security event logging
    /// </summary>
    public static void LogSecurityEvent(this ILogger logger, string eventType, string? userId = null, string? ipAddress = null, Dictionary<string, object>? context = null)
    {
        using var activity = Activity.Current?.Source.StartActivity($"Security.{eventType}");
        activity?.SetTag("event.type", "security");
        activity?.SetTag("user.id", userId);
        activity?.SetTag("client.ip", ipAddress);

        logger.LogWarning("Security event: {EventType} - User: {UserId}, IP: {IpAddress}, Context: {@Context}", 
            eventType, userId ?? "unknown", ipAddress ?? "unknown", context);
    }

    /// <summary>
    /// External API call logging
    /// </summary>
    public static void LogExternalApiCall(this ILogger logger, string serviceName, string endpoint, long durationMs, int statusCode, string? requestId = null)
    {
        using var activity = Activity.Current?.Source.StartActivity($"External.{serviceName}");
        activity?.SetTag("external.service", serviceName);
        activity?.SetTag("external.endpoint", endpoint);
        activity?.SetTag("external.duration_ms", durationMs);
        activity?.SetTag("external.status_code", statusCode);
        activity?.SetTag("external.request_id", requestId);

        var logLevel = statusCode >= 400 ? LogLevel.Warning : LogLevel.Information;
        logger.Log(logLevel, "External API call: {ServiceName}{Endpoint} - {StatusCode} in {Duration}ms (Request ID: {RequestId})", 
            serviceName, endpoint, statusCode, durationMs, requestId);
    }

    /// <summary>
    /// Database operation logging
    /// </summary>
    public static void LogDatabaseOperation(this ILogger logger, string operation, string tableName, long durationMs, int? affectedRows = null)
    {
        using var activity = Activity.Current?.Source.StartActivity($"Database.{operation}");
        activity?.SetTag("db.operation", operation);
        activity?.SetTag("db.table", tableName);
        activity?.SetTag("db.duration_ms", durationMs);
        activity?.SetTag("db.affected_rows", affectedRows);

        logger.LogInformation("Database operation: {Operation} on {TableName} completed in {Duration}ms, affected {AffectedRows} rows", 
            operation, tableName, durationMs, affectedRows ?? 0);
    }

    /// <summary>
    /// Event publishing/consuming logging
    /// </summary>
    public static void LogEventOperation(this ILogger logger, string operation, string eventType, string? eventId = null, long? durationMs = null)
    {
        using var activity = Activity.Current?.Source.StartActivity($"Event.{operation}");
        activity?.SetTag("event.operation", operation);
        activity?.SetTag("event.type", eventType);
        activity?.SetTag("event.id", eventId);
        activity?.SetTag("event.duration_ms", durationMs);

        logger.LogInformation("Event {Operation}: {EventType} (ID: {EventId}) {Duration}", 
            operation, eventType, eventId ?? "unknown", 
            durationMs.HasValue ? $"in {durationMs}ms" : "");
    }

    /// <summary>
    /// Cache operation logging
    /// </summary>
    public static void LogCacheOperation(this ILogger logger, string operation, string key, bool hit = false, long? durationMs = null)
    {
        using var activity = Activity.Current?.Source.StartActivity($"Cache.{operation}");
        activity?.SetTag("cache.operation", operation);
        activity?.SetTag("cache.key", key);
        activity?.SetTag("cache.hit", hit);
        activity?.SetTag("cache.duration_ms", durationMs);

        logger.LogInformation("Cache {Operation}: {Key} - {Result} {Duration}", 
            operation, key, hit ? "HIT" : "MISS", 
            durationMs.HasValue ? $"in {durationMs}ms" : "");
    }

    /// <summary>
    /// Error logging with context
    /// </summary>
    public static void LogError(this ILogger logger, Exception exception, string operation, Dictionary<string, object>? context = null)
    {
        using var activity = Activity.Current?.Source.StartActivity($"Error.{operation}");
        activity?.SetStatus(ActivityStatusCode.Error, exception.Message);
        activity?.SetTag("error.type", exception.GetType().Name);
        activity?.SetTag("error.operation", operation);

        if (context != null)
        {
            foreach (var item in context)
            {
                activity?.SetTag($"error.context.{item.Key}", item.Value?.ToString());
            }
        }

        logger.LogError(exception, "Error in operation {Operation} with context {@Context}", operation, context);
    }
}