using System.Diagnostics.Metrics;
using Microsoft.Extensions.Logging;

namespace Vitrin.Shared.Infrastructure.Observability;

/// <summary>
/// Vitrin için özel business metrics collector
/// </summary>
public class MetricsCollector : IDisposable
{
    private readonly Meter _meter;
    private readonly ILogger<MetricsCollector> _logger;

    // Counters
    private readonly Counter<long> _userRegistrations;
    private readonly Counter<long> _productSubmissions;
    private readonly Counter<long> _votes;
    private readonly Counter<long> _comments;
    private readonly Counter<long> _authAttempts;
    private readonly Counter<long> _emailsSent;
    private readonly Counter<long> _cacheHits;
    private readonly Counter<long> _cacheMisses;
    private readonly Counter<long> _eventPublished;
    private readonly Counter<long> _eventConsumed;
    private readonly Counter<long> _errors;

    // Gauges
    private readonly UpDownCounter<long> _activeUsers;
    private readonly UpDownCounter<long> _pendingProducts;
    private readonly UpDownCounter<long> _queueSize;

    // Histograms
    private readonly Histogram<double> _requestDuration;
    private readonly Histogram<double> _databaseQueryDuration;
    private readonly Histogram<double> _cacheOperationDuration;
    private readonly Histogram<double> _eventProcessingDuration;

    public MetricsCollector(string serviceName, ILogger<MetricsCollector> logger)
    {
        _logger = logger;
        _meter = new Meter($"Vitrin.{serviceName}", "1.0.0");

        // Initialize counters
        _userRegistrations = _meter.CreateCounter<long>("vitrin_user_registrations_total", "count", "Total number of user registrations");
        _productSubmissions = _meter.CreateCounter<long>("vitrin_product_submissions_total", "count", "Total number of product submissions");
        _votes = _meter.CreateCounter<long>("vitrin_votes_total", "count", "Total number of votes cast");
        _comments = _meter.CreateCounter<long>("vitrin_comments_total", "count", "Total number of comments posted");
        _authAttempts = _meter.CreateCounter<long>("vitrin_auth_attempts_total", "count", "Total number of authentication attempts");
        _emailsSent = _meter.CreateCounter<long>("vitrin_emails_sent_total", "count", "Total number of emails sent");
        _cacheHits = _meter.CreateCounter<long>("vitrin_cache_hits_total", "count", "Total number of cache hits");
        _cacheMisses = _meter.CreateCounter<long>("vitrin_cache_misses_total", "count", "Total number of cache misses");
        _eventPublished = _meter.CreateCounter<long>("vitrin_events_published_total", "count", "Total number of events published");
        _eventConsumed = _meter.CreateCounter<long>("vitrin_events_consumed_total", "count", "Total number of events consumed");
        _errors = _meter.CreateCounter<long>("vitrin_errors_total", "count", "Total number of errors");

        // Initialize gauges
        _activeUsers = _meter.CreateUpDownCounter<long>("vitrin_active_users", "count", "Current number of active users");
        _pendingProducts = _meter.CreateUpDownCounter<long>("vitrin_pending_products", "count", "Current number of pending products");
        _queueSize = _meter.CreateUpDownCounter<long>("vitrin_queue_size", "count", "Current size of processing queues");

        // Initialize histograms
        _requestDuration = _meter.CreateHistogram<double>("vitrin_request_duration_seconds", "seconds", "HTTP request duration");
        _databaseQueryDuration = _meter.CreateHistogram<double>("vitrin_database_query_duration_seconds", "seconds", "Database query duration");
        _cacheOperationDuration = _meter.CreateHistogram<double>("vitrin_cache_operation_duration_seconds", "seconds", "Cache operation duration");
        _eventProcessingDuration = _meter.CreateHistogram<double>("vitrin_event_processing_duration_seconds", "seconds", "Event processing duration");
    }

    // Business Metrics
    public void IncrementUserRegistrations(string source = "direct")
    {
        _userRegistrations.Add(1, new KeyValuePair<string, object?>("source", source));
        _logger.LogInformation("User registration metric incremented from source: {Source}", source);
    }

    public void IncrementProductSubmissions(string category = "unknown")
    {
        _productSubmissions.Add(1, new KeyValuePair<string, object?>("category", category));
        _logger.LogInformation("Product submission metric incremented for category: {Category}", category);
    }

    public void IncrementVotes(string productId, string voteType = "upvote")
    {
        _votes.Add(1, 
            new KeyValuePair<string, object?>("product_id", productId),
            new KeyValuePair<string, object?>("vote_type", voteType));
        _logger.LogInformation("Vote metric incremented: {VoteType} for product {ProductId}", voteType, productId);
    }

    public void IncrementComments(string productId)
    {
        _comments.Add(1, new KeyValuePair<string, object?>("product_id", productId));
        _logger.LogInformation("Comment metric incremented for product: {ProductId}", productId);
    }

    public void IncrementAuthAttempts(bool successful, string method = "password")
    {
        _authAttempts.Add(1,
            new KeyValuePair<string, object?>("success", successful),
            new KeyValuePair<string, object?>("method", method));
        _logger.LogInformation("Auth attempt metric incremented: {Success} via {Method}", successful, method);
    }

    public void IncrementEmailsSent(string type = "notification")
    {
        _emailsSent.Add(1, new KeyValuePair<string, object?>("type", type));
        _logger.LogInformation("Email sent metric incremented for type: {Type}", type);
    }

    // Cache Metrics
    public void IncrementCacheHits(string operation)
    {
        _cacheHits.Add(1, new KeyValuePair<string, object?>("operation", operation));
    }

    public void IncrementCacheMisses(string operation)
    {
        _cacheMisses.Add(1, new KeyValuePair<string, object?>("operation", operation));
    }

    public void RecordCacheOperationDuration(double durationSeconds, string operation)
    {
        _cacheOperationDuration.Record(durationSeconds, new KeyValuePair<string, object?>("operation", operation));
    }

    // Event Metrics
    public void IncrementEventsPublished(string eventType)
    {
        _eventPublished.Add(1, new KeyValuePair<string, object?>("event_type", eventType));
        _logger.LogInformation("Event published metric incremented for type: {EventType}", eventType);
    }

    public void IncrementEventsConsumed(string eventType, bool successful = true)
    {
        _eventConsumed.Add(1,
            new KeyValuePair<string, object?>("event_type", eventType),
            new KeyValuePair<string, object?>("success", successful));
        _logger.LogInformation("Event consumed metric incremented: {EventType} - {Success}", eventType, successful);
    }

    public void RecordEventProcessingDuration(double durationSeconds, string eventType)
    {
        _eventProcessingDuration.Record(durationSeconds, new KeyValuePair<string, object?>("event_type", eventType));
    }

    // Error Metrics
    public void IncrementErrors(string errorType, string operation = "unknown")
    {
        _errors.Add(1,
            new KeyValuePair<string, object?>("error_type", errorType),
            new KeyValuePair<string, object?>("operation", operation));
        _logger.LogWarning("Error metric incremented: {ErrorType} in operation {Operation}", errorType, operation);
    }

    // Gauge Operations
    public void SetActiveUsers(long count)
    {
        // Reset gauge to new value
        _activeUsers.Add(count - GetCurrentActiveUsers());
        _logger.LogInformation("Active users gauge set to: {Count}", count);
    }

    public void SetPendingProducts(long count)
    {
        _pendingProducts.Add(count - GetCurrentPendingProducts());
        _logger.LogInformation("Pending products gauge set to: {Count}", count);
    }

    public void SetQueueSize(long size, string queueName = "default")
    {
        _queueSize.Add(size, new KeyValuePair<string, object?>("queue_name", queueName));
        _logger.LogInformation("Queue size set to {Size} for queue: {QueueName}", size, queueName);
    }

    // Duration Recording
    public void RecordRequestDuration(double durationSeconds, string method, string endpoint, int statusCode)
    {
        _requestDuration.Record(durationSeconds,
            new KeyValuePair<string, object?>("method", method),
            new KeyValuePair<string, object?>("endpoint", endpoint),
            new KeyValuePair<string, object?>("status_code", statusCode));
    }

    public void RecordDatabaseQueryDuration(double durationSeconds, string operation, string table = "unknown")
    {
        _databaseQueryDuration.Record(durationSeconds,
            new KeyValuePair<string, object?>("operation", operation),
            new KeyValuePair<string, object?>("table", table));
    }

    // Helper methods for gauge calculations (these would normally query current state)
    private long GetCurrentActiveUsers() => 0; // Implement based on your needs
    private long GetCurrentPendingProducts() => 0; // Implement based on your needs

    public void Dispose()
    {
        _meter?.Dispose();
        GC.SuppressFinalize(this);
    }
}