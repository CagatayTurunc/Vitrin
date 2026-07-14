using System.Text;
using System.Text.Json;
using Confluent.Kafka;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Vitrin.Shared.Infrastructure.Kafka;

/// <summary>
/// Reusable at-least-once Kafka consumer with bounded retry, exponential backoff,
/// explicit offset commits, and a per-topic dead-letter topic.
/// </summary>
public abstract class KafkaConsumerBase : BackgroundService
{
    private readonly IConsumer<string, string> _consumer;
    private readonly IProducer<string, string> _deadLetterProducer;
    private readonly ILogger _logger;
    private readonly IReadOnlyList<string> _topics;
    private readonly string _groupId;
    private readonly int _maxRetryAttempts;
    private readonly int _initialRetryDelayMs;
    private readonly int _maxRetryDelayMs;

    protected KafkaConsumerBase(
        IConfiguration configuration,
        ILogger logger,
        string topic,
        string groupId)
        : this(configuration, logger, [topic], groupId)
    {
    }

    protected KafkaConsumerBase(
        IConfiguration configuration,
        ILogger logger,
        IReadOnlyList<string> topics,
        string groupId)
    {
        if (topics.Count == 0)
        {
            throw new ArgumentException("At least one Kafka topic is required.", nameof(topics));
        }

        _topics = topics;
        _groupId = groupId;
        _logger = logger;
        _maxRetryAttempts = ReadPositiveInt(configuration, "Kafka:Consumer:MaxRetryAttempts", 5);
        _initialRetryDelayMs = ReadPositiveInt(configuration, "Kafka:Consumer:InitialRetryDelayMs", 500);
        _maxRetryDelayMs = ReadPositiveInt(configuration, "Kafka:Consumer:MaxRetryDelayMs", 30_000);

        var bootstrapServers = configuration["Kafka:BootstrapServers"] ?? "localhost:9092";
        var consumerConfig = new ConsumerConfig
        {
            BootstrapServers = bootstrapServers,
            GroupId = groupId,
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = false,
            EnableAutoOffsetStore = false,
            SessionTimeoutMs = 30_000,
            HeartbeatIntervalMs = 10_000,
            MaxPollIntervalMs = 300_000
        };

        _consumer = new ConsumerBuilder<string, string>(consumerConfig)
            .SetErrorHandler((_, error) =>
                _logger.LogError("Kafka consumer error [{Code}]: {Reason}", error.Code, error.Reason))
            .SetPartitionsAssignedHandler((_, partitions) =>
                _logger.LogInformation("Assigned partitions: {Partitions}", string.Join(", ", partitions)))
            .SetPartitionsRevokedHandler((_, partitions) =>
                _logger.LogInformation("Revoked partitions: {Partitions}", string.Join(", ", partitions)))
            .Build();

        _deadLetterProducer = new ProducerBuilder<string, string>(new ProducerConfig
        {
            BootstrapServers = bootstrapServers,
            Acks = Acks.All,
            EnableIdempotence = true
        }).Build();
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation(
            "[{GroupId}] Starting Kafka consumer for topics {Topics}",
            _groupId,
            string.Join(", ", _topics));

        _consumer.Subscribe(_topics);

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                ConsumeResult<string, string>? result = null;
                try
                {
                    result = _consumer.Consume(TimeSpan.FromMilliseconds(500));
                    if (result is null)
                    {
                        continue;
                    }

                    await ProcessWithRetryAsync(result, stoppingToken);

                    _consumer.Commit(result);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (ConsumeException ex)
                {
                    _logger.LogError(ex, "[{GroupId}] Kafka consume error", _groupId);
                    await Task.Delay(_initialRetryDelayMs, stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(
                        ex,
                        "[{GroupId}] Message processing could not be completed. Topic={Topic}, Key={Key}",
                        _groupId,
                        result?.Topic ?? "?",
                        result?.Message.Key ?? "?");

                    if (result is not null)
                    {
                        _consumer.Seek(result.TopicPartitionOffset);
                    }

                    await Task.Delay(_initialRetryDelayMs, stoppingToken);
                }
            }
        }
        finally
        {
            _consumer.Close();
            _logger.LogInformation("[{GroupId}] Kafka consumer stopped.", _groupId);
        }
    }

    private async Task ProcessWithRetryAsync(
        ConsumeResult<string, string> result,
        CancellationToken cancellationToken)
    {
        for (var attempt = 1; attempt <= _maxRetryAttempts; attempt++)
        {
            try
            {
                await ProcessMessageAsync(result.Message.Key, result.Message.Value, cancellationToken);
                return;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex) when (attempt < _maxRetryAttempts)
            {
                var delay = CalculateBackoff(attempt);
                _logger.LogWarning(
                    ex,
                    "[{GroupId}] Processing attempt {Attempt}/{MaxAttempts} failed. " +
                    "Topic={Topic}, Partition={Partition}, Offset={Offset}, RetryDelayMs={RetryDelayMs}",
                    _groupId,
                    attempt,
                    _maxRetryAttempts,
                    result.Topic,
                    result.Partition,
                    result.Offset,
                    delay);
                await Task.Delay(delay, cancellationToken);
            }
            catch (Exception ex)
            {
                await PublishDeadLetterAsync(result, ex, cancellationToken);
                _logger.LogError(
                    ex,
                    "[{GroupId}] Poison message moved to {DeadLetterTopic}. Key={Key}",
                    _groupId,
                    $"{result.Topic}.dlq",
                    result.Message.Key);
                return;
            }
        }
    }

    private async Task PublishDeadLetterAsync(
        ConsumeResult<string, string> result,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var headers = CloneHeaders(result.Message.Headers);
        headers.Add("dlq-original-topic", Encoding.UTF8.GetBytes(result.Topic));
        headers.Add("dlq-original-partition", Encoding.UTF8.GetBytes(result.Partition.Value.ToString()));
        headers.Add("dlq-original-offset", Encoding.UTF8.GetBytes(result.Offset.Value.ToString()));
        headers.Add("dlq-consumer-group", Encoding.UTF8.GetBytes(_groupId));
        headers.Add("dlq-error-type", Encoding.UTF8.GetBytes(exception.GetType().Name));
        headers.Add("dlq-error", Encoding.UTF8.GetBytes(Truncate(exception.Message, 1_000)));
        headers.Add("dlq-failed-at", Encoding.UTF8.GetBytes(DateTime.UtcNow.ToString("O")));

        await _deadLetterProducer.ProduceAsync(
            $"{result.Topic}.dlq",
            new Message<string, string>
            {
                Key = result.Message.Key,
                Value = result.Message.Value,
                Headers = headers
            },
            cancellationToken);
    }

    private int CalculateBackoff(int attempt)
    {
        var multiplier = Math.Pow(2, attempt - 1);
        return (int)Math.Min(_initialRetryDelayMs * multiplier, _maxRetryDelayMs);
    }

    private static Headers CloneHeaders(Headers? source)
    {
        var headers = new Headers();
        if (source is null)
        {
            return headers;
        }

        foreach (var header in source)
        {
            headers.Add(header.Key, header.GetValueBytes());
        }

        return headers;
    }

    private static int ReadPositiveInt(
        IConfiguration configuration,
        string key,
        int defaultValue) =>
        int.TryParse(configuration[key], out var value) && value > 0
            ? value
            : defaultValue;

    private static string Truncate(string value, int maxLength) =>
        value.Length <= maxLength ? value : value[..maxLength];

    protected abstract Task ProcessMessageAsync(
        string key,
        string value,
        CancellationToken cancellationToken);

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
        _consumer.Dispose();
        _deadLetterProducer.Dispose();
        base.Dispose();
    }
}
