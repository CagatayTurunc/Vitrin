using System.Text;
using System.Text.Json;
using Confluent.Kafka;
using Confluent.Kafka.Admin;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Vitrin.IntegrationTests.Infrastructure;
using Vitrin.Shared.Contracts.Events;
using Vitrin.Shared.Infrastructure.Kafka;

namespace Vitrin.IntegrationTests.Kafka;

[Collection(KafkaCollection.Name)]
[Trait("Category", "Integration")]
public sealed class KafkaDeliveryTests(KafkaFixture fixture)
{
    [Fact]
    public async Task Producer_publishes_catalog_topic_payload_and_trace_headers()
    {
        await EnsureTopicAsync(EventTopics.Voting);
        var configuration = CreateConfiguration();
        using var producer = new KafkaProducer(configuration, NullLogger<KafkaProducer>.Instance);
        using var consumer = CreateConsumer();
        consumer.Subscribe(EventTopics.Voting);

        var integrationEvent = new VoteAddedEvent
        {
            VoteId = Guid.NewGuid(),
            ProductId = Guid.NewGuid(),
            UserId = Guid.NewGuid()
        };

        await producer.PublishAsync(integrationEvent);
        var result = ConsumeUntil(consumer, message => message.Message.Key == integrationEvent.EventId.ToString());

        result.Topic.Should().Be(EventTopics.Voting);
        result.Message.Headers.GetLastBytes("event-id").Should().Equal(Encoding.UTF8.GetBytes(integrationEvent.EventId.ToString()));
        result.Message.Headers.GetLastBytes("event-type").Should().Equal(Encoding.UTF8.GetBytes(integrationEvent.EventType));
        result.Message.Headers.GetLastBytes("event-version").Should().Equal(Encoding.UTF8.GetBytes(integrationEvent.Version));
        result.Message.Headers.GetLastBytes("correlation-id").Should().Equal(Encoding.UTF8.GetBytes(integrationEvent.CorrelationId.ToString()));

        using var payload = JsonDocument.Parse(result.Message.Value);
        payload.RootElement.GetProperty(nameof(VoteAddedEvent.ProductId)).GetGuid().Should().Be(integrationEvent.ProductId);
    }

    [Fact]
    public async Task Poison_message_is_retried_then_published_to_dead_letter_topic()
    {
        var topic = $"integration-poison-{Guid.NewGuid():N}";
        await EnsureTopicAsync(topic);
        await EnsureTopicAsync($"{topic}.dlq");

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Kafka:BootstrapServers"] = fixture.BootstrapServers,
                ["Kafka:Consumer:MaxRetryAttempts"] = "2",
                ["Kafka:Consumer:InitialRetryDelayMs"] = "10",
                ["Kafka:Consumer:MaxRetryDelayMs"] = "20"
            })
            .Build();
        using var poisonConsumer = new AlwaysFailingConsumer(configuration, topic);
        await poisonConsumer.StartAsync(CancellationToken.None);

        using var dlqConsumer = CreateConsumer();
        dlqConsumer.Subscribe($"{topic}.dlq");
        using var producer = new ProducerBuilder<string, string>(new ProducerConfig
        {
            BootstrapServers = fixture.BootstrapServers,
            Acks = Acks.All
        }).Build();

        var key = Guid.NewGuid().ToString();
        await producer.ProduceAsync(topic, new Message<string, string> { Key = key, Value = "{\"invalid\":true}" });
        var deadLetter = ConsumeUntil(dlqConsumer, message => message.Message.Key == key);

        poisonConsumer.Attempts.Should().Be(2);
        Encoding.UTF8.GetString(deadLetter.Message.Headers.GetLastBytes("dlq-original-topic")).Should().Be(topic);
        Encoding.UTF8.GetString(deadLetter.Message.Headers.GetLastBytes("dlq-consumer-group")).Should().StartWith("integration-poison-");
        Encoding.UTF8.GetString(deadLetter.Message.Headers.GetLastBytes("dlq-error-type")).Should().Be(nameof(InvalidOperationException));

        await poisonConsumer.StopAsync(CancellationToken.None);
    }

    private IConfiguration CreateConfiguration() => new ConfigurationBuilder()
        .AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Kafka:BootstrapServers"] = fixture.BootstrapServers
        })
        .Build();

    private IConsumer<string, string> CreateConsumer() =>
        new ConsumerBuilder<string, string>(new ConsumerConfig
        {
            BootstrapServers = fixture.BootstrapServers,
            GroupId = $"integration-{Guid.NewGuid():N}",
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = false
        }).Build();

    private async Task EnsureTopicAsync(string topic)
    {
        using var admin = new AdminClientBuilder(new AdminClientConfig
        {
            BootstrapServers = fixture.BootstrapServers
        }).Build();

        try
        {
            await admin.CreateTopicsAsync([
                new TopicSpecification { Name = topic, NumPartitions = 1, ReplicationFactor = 1 }
            ]);
        }
        catch (CreateTopicsException exception) when (
            exception.Results.All(result => result.Error.Code == ErrorCode.TopicAlreadyExists))
        {
        }
    }

    private static ConsumeResult<string, string> ConsumeUntil(
        IConsumer<string, string> consumer,
        Func<ConsumeResult<string, string>, bool> predicate)
    {
        var deadline = DateTime.UtcNow.AddSeconds(30);
        while (DateTime.UtcNow < deadline)
        {
            var result = consumer.Consume(TimeSpan.FromMilliseconds(500));
            if (result is not null && predicate(result))
            {
                return result;
            }
        }

        throw new TimeoutException("Expected Kafka message was not consumed within 30 seconds.");
    }

    private sealed class AlwaysFailingConsumer(IConfiguration configuration, string topic)
        : KafkaConsumerBase(
            configuration,
            NullLogger<AlwaysFailingConsumer>.Instance,
            topic,
            $"integration-poison-{Guid.NewGuid():N}")
    {
        private int _attempts;

        public int Attempts => Volatile.Read(ref _attempts);

        protected override Task ProcessMessageAsync(string key, string value, CancellationToken cancellationToken)
        {
            Interlocked.Increment(ref _attempts);
            throw new InvalidOperationException("Deliberate poison message for the DLQ integration test.");
        }
    }
}

internal static class KafkaHeaderExtensions
{
    public static byte[] GetLastBytes(this Headers headers, string key) =>
        headers.Last(header => header.Key == key).GetValueBytes();
}
