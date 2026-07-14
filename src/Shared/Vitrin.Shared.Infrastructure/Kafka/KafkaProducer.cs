using System.Text;
using System.Text.Json;
using Confluent.Kafka;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Vitrin.Shared.Contracts.Events;

namespace Vitrin.Shared.Infrastructure.Kafka;

public sealed record SerializedIntegrationEvent(
    Guid EventId,
    string EventType,
    string Version,
    DateTime OccurredAtUtc,
    Guid CorrelationId,
    Guid? CausationId,
    string Topic,
    string Payload);

public interface IEventPublisher
{
    Task PublishAsync<TEvent>(
        TEvent @event,
        CancellationToken cancellationToken = default)
        where TEvent : IEvent;

    Task PublishRawAsync(
        SerializedIntegrationEvent @event,
        CancellationToken cancellationToken = default);
}

public sealed class KafkaProducer : IEventPublisher, IDisposable
{
    private readonly IProducer<string, string> _producer;
    private readonly ILogger<KafkaProducer> _logger;

    public KafkaProducer(IConfiguration configuration, ILogger<KafkaProducer> logger)
    {
        var config = new ProducerConfig
        {
            BootstrapServers = configuration["Kafka:BootstrapServers"] ?? "localhost:9092",
            Acks = Acks.All,
            EnableIdempotence = true,
            CompressionType = CompressionType.Snappy
        };

        _producer = new ProducerBuilder<string, string>(config).Build();
        _logger = logger;
    }

    public Task PublishAsync<TEvent>(
        TEvent @event,
        CancellationToken cancellationToken = default)
        where TEvent : IEvent =>
        PublishRawAsync(
            new SerializedIntegrationEvent(
                @event.EventId,
                @event.EventType,
                @event.Version,
                @event.Timestamp,
                @event.CorrelationId,
                @event.CausationId,
                EventCatalog.GetTopic(@event),
                JsonSerializer.Serialize(@event, @event.GetType())),
            cancellationToken);

    public async Task PublishRawAsync(
        SerializedIntegrationEvent @event,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _producer.ProduceAsync(
                @event.Topic,
                new Message<string, string>
                {
                    Key = @event.EventId.ToString(),
                    Value = @event.Payload,
                    Headers = CreateHeaders(@event)
                },
                cancellationToken);

            _logger.LogInformation(
                "Event {EventType} ({EventId}) published to {Topic} at offset {Offset}",
                @event.EventType,
                @event.EventId,
                @event.Topic,
                result.Offset);
        }
        catch (ProduceException<string, string> ex)
        {
            _logger.LogError(
                ex,
                "Failed to publish event {EventType} ({EventId}) to {Topic}",
                @event.EventType,
                @event.EventId,
                @event.Topic);
            throw;
        }
    }

    private static Headers CreateHeaders(SerializedIntegrationEvent @event)
    {
        var headers = new Headers
        {
            { "content-type", Encoding.UTF8.GetBytes("application/json") },
            { "event-id", Encoding.UTF8.GetBytes(@event.EventId.ToString()) },
            { "event-type", Encoding.UTF8.GetBytes(@event.EventType) },
            { "event-version", Encoding.UTF8.GetBytes(@event.Version) },
            { "correlation-id", Encoding.UTF8.GetBytes(@event.CorrelationId.ToString()) },
            { "timestamp", Encoding.UTF8.GetBytes(@event.OccurredAtUtc.ToString("O")) }
        };

        if (@event.CausationId.HasValue)
        {
            headers.Add("causation-id", Encoding.UTF8.GetBytes(@event.CausationId.Value.ToString()));
        }

        return headers;
    }

    public void Dispose()
    {
        _producer.Dispose();
    }
}
