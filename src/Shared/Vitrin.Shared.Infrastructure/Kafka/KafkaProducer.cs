using System.Text.Json;
using System.Text;
using Confluent.Kafka;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Vitrin.Shared.Contracts.Events;

namespace Vitrin.Shared.Infrastructure.Kafka;

public interface IEventPublisher
{
    Task PublishAsync<TEvent>(TEvent @event) where TEvent : IEvent;
}

public class KafkaProducer : IEventPublisher, IDisposable
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
    
    public async Task PublishAsync<TEvent>(TEvent @event) where TEvent : IEvent
    {
        var topicName = GetTopicName(typeof(TEvent));
        var message = JsonSerializer.Serialize(@event);
        
        try
        {
            var result = await _producer.ProduceAsync(topicName, new Message<string, string>
            {
                Key = @event.EventId.ToString(),
                Value = message,
                Headers = new Headers
                {
                    { "event-type", Encoding.UTF8.GetBytes(@event.EventType) },
                    { "correlation-id", Encoding.UTF8.GetBytes(Guid.NewGuid().ToString()) },
                    { "timestamp", Encoding.UTF8.GetBytes(DateTime.UtcNow.ToString("O")) }
                }
            });
            
            _logger.LogInformation(
                "Event {EventType} published to {Topic} at offset {Offset}", 
                @event.EventType, topicName, result.Offset);
        }
        catch (ProduceException<string, string> ex)
        {
            _logger.LogError(ex, 
                "Failed to publish event {EventType} to {Topic}", 
                @event.EventType, topicName);
            throw;
        }
    }
    
    private string GetTopicName(Type eventType)
    {
        // Extract service name from namespace
        // Vitrin.Auth.Contracts.Events.UserRegisteredEvent -> "auth-events"
        // If it's from Shared.Contracts, we might just use shared-events or parse it.
        var parts = eventType.Namespace?.Split('.') ?? Array.Empty<string>();
        var serviceName = parts.Length > 1 ? parts[1].ToLowerInvariant() : "unknown";
        return $"{serviceName}-events";
    }
    
    public void Dispose()
    {
        _producer?.Dispose();
    }
}
