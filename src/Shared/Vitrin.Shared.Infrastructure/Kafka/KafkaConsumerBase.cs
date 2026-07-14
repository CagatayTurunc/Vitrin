using System.Text.Json;
using Confluent.Kafka;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Vitrin.Shared.Infrastructure.Kafka;

/// <summary>
/// Kafka consumer için tekrar kullanılabilir base class.
/// Her servis bu sınıftan türeterek kendi topic'ini consume eder.
/// </summary>
public abstract class KafkaConsumerBase : BackgroundService
{
    private readonly IConsumer<string, string> _consumer;
    private readonly ILogger _logger;
    private readonly string _topic;
    private readonly string _groupId;

    protected KafkaConsumerBase(
        IConfiguration configuration,
        ILogger logger,
        string topic,
        string groupId)
    {
        _topic = topic;
        _groupId = groupId;
        _logger = logger;

        var config = new ConsumerConfig
        {
            BootstrapServers = configuration["Kafka:BootstrapServers"] ?? "localhost:9092",
            GroupId = groupId,
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = false,           // Manuel commit — at-least-once garantisi
            EnableAutoOffsetStore = false,
            SessionTimeoutMs = 30000,
            HeartbeatIntervalMs = 10000,
            MaxPollIntervalMs = 300000
        };

        _consumer = new ConsumerBuilder<string, string>(config)
            .SetErrorHandler((_, e) =>
                _logger.LogError("Kafka consumer error [{Code}]: {Reason}", e.Code, e.Reason))
            .SetPartitionsAssignedHandler((c, partitions) =>
                _logger.LogInformation("Assigned partitions: {Partitions}", string.Join(", ", partitions)))
            .SetPartitionsRevokedHandler((c, partitions) =>
                _logger.LogInformation("Revoked partitions: {Partitions}", string.Join(", ", partitions)))
            .Build();
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation(
            "[{GroupId}] Starting Kafka consumer for topic '{Topic}'",
            _groupId, _topic);

        _consumer.Subscribe(_topic);

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                ConsumeResult<string, string>? result = null;
                try
                {
                    result = _consumer.Consume(TimeSpan.FromMilliseconds(500));

                    if (result is null)
                        continue;

                    _logger.LogDebug(
                        "[{GroupId}] Received message: Key={Key}, Offset={Offset}",
                        _groupId, result.Message.Key, result.Offset);

                    await ProcessMessageAsync(result.Message.Key, result.Message.Value, stoppingToken);

                    // Başarıyla işlendikten sonra commit
                    _consumer.StoreOffset(result);
                    _consumer.Commit(result);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (ConsumeException ex)
                {
                    _logger.LogError(ex, "[{GroupId}] Consume error on topic '{Topic}'", _groupId, _topic);
                    await Task.Delay(1000, stoppingToken); // kısa bekle, tekrar dene
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex,
                        "[{GroupId}] Error processing message from topic '{Topic}', Key={Key}",
                        _groupId, result?.Message.Key ?? "?", _topic);
                    // İşleme hatası varsa mesajı atla (commit etme → tekrar denenecek)
                    // Production'da Dead Letter Queue kullanılabilir
                }
            }
        }
        finally
        {
            _consumer.Close();
            _logger.LogInformation("[{GroupId}] Kafka consumer stopped.", _groupId);
        }
    }

    /// <summary>
    /// Türeten sınıf bu metodu implement eder.
    /// </summary>
    protected abstract Task ProcessMessageAsync(string key, string value, CancellationToken cancellationToken);

    /// <summary>
    /// JSON mesajı belirtilen tipe deserialize eder.
    /// </summary>
    protected static T? DeserializeMessage<T>(string value)
    {
        try
        {
            return JsonSerializer.Deserialize<T>(value, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
        }
        catch
        {
            return default;
        }
    }

    public override void Dispose()
    {
        _consumer?.Dispose();
        base.Dispose();
    }
}
