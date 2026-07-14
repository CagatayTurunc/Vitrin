using System.Text.Json;
using FluentAssertions;
using Vitrin.Shared.Contracts.Events;
using Vitrin.Shared.Infrastructure.Inbox;
using Vitrin.Shared.Infrastructure.Outbox;
using Xunit;

namespace Vitrin.Auth.Tests.Infrastructure;

public sealed class EventDeliveryContractsTests
{
    [Fact]
    public void Catalog_Should_Register_Every_Concrete_Integration_Event()
    {
        var integrationEventTypes = typeof(BaseEvent).Assembly
            .GetTypes()
            .Where(type => !type.IsAbstract && typeof(BaseEvent).IsAssignableFrom(type))
            .ToHashSet();

        EventCatalog.Entries.Keys.Should().BeEquivalentTo(integrationEventTypes);
    }

    [Theory]
    [InlineData(typeof(VoteAddedEvent), EventTopics.Voting)]
    [InlineData(typeof(VoteRemovedEvent), EventTopics.Voting)]
    [InlineData(typeof(SendNotificationEvent), EventTopics.Notification)]
    [InlineData(typeof(ProductViewedEvent), EventTopics.Analytics)]
    [InlineData(typeof(ProductPublishedEvent), EventTopics.Social)]
    [InlineData(typeof(UserRegisteredEvent), EventTopics.User)]
    public void Catalog_Should_Route_Events_To_Their_Semantic_Topic(
        Type eventType,
        string expectedTopic)
    {
        EventCatalog.GetTopic(eventType).Should().Be(expectedTopic);
    }

    [Fact]
    public void Catalog_Should_Reject_Unregistered_Event_Types()
    {
        var action = () => EventCatalog.GetTopic(typeof(UnregisteredEvent));

        action.Should()
            .Throw<InvalidOperationException>()
            .WithMessage("*not registered*");
    }

    [Fact]
    public void Serialization_Should_Preserve_Event_Envelope_Metadata()
    {
        var eventId = Guid.NewGuid();
        var correlationId = Guid.NewGuid();
        var causationId = Guid.NewGuid();
        var timestamp = new DateTime(2026, 7, 14, 12, 30, 0, DateTimeKind.Utc);
        var source = new VoteAddedEvent
        {
            EventId = eventId,
            CorrelationId = correlationId,
            CausationId = causationId,
            Timestamp = timestamp,
            VoteId = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            ProductId = Guid.NewGuid()
        };

        var json = JsonSerializer.Serialize(source);
        var restored = JsonSerializer.Deserialize<VoteAddedEvent>(json);

        restored.Should().NotBeNull();
        restored!.EventId.Should().Be(eventId);
        restored.Timestamp.Should().Be(timestamp);
        restored.EventType.Should().Be("voting.vote_added");
        restored.Version.Should().Be("1.0");
        restored.CorrelationId.Should().Be(correlationId);
        restored.CausationId.Should().Be(causationId);
    }

    [Fact]
    public void Outbox_Should_Capture_The_Complete_Event_Envelope()
    {
        var createdAt = new DateTime(2026, 7, 14, 13, 0, 0, DateTimeKind.Utc);
        var source = new ProductPublishedEvent
        {
            ProductId = Guid.NewGuid(),
            MakerId = Guid.NewGuid(),
            ProductName = "Vitrin",
            ProductSlug = "vitrin"
        };

        var message = OutboxMessage.Create(source, createdAt);

        message.Id.Should().Be(source.EventId);
        message.Topic.Should().Be(EventTopics.Social);
        message.EventType.Should().Be(source.EventType);
        message.EventVersion.Should().Be(source.Version);
        message.CorrelationId.Should().Be(source.CorrelationId);
        message.CreatedAtUtc.Should().Be(createdAt);
        message.Payload.Should().Contain(source.ProductId.ToString());
        message.ProcessedAtUtc.Should().BeNull();
    }

    [Fact]
    public void Outbox_Should_Use_Bounded_Exponential_Backoff_Then_DeadLetter()
    {
        var now = new DateTime(2026, 7, 14, 14, 0, 0, DateTimeKind.Utc);
        var message = OutboxMessage.Create(new VoteRemovedEvent(), now);

        message.MarkFailed("first", now, maxRetryAttempts: 3, TimeSpan.FromSeconds(3));
        message.RetryCount.Should().Be(1);
        message.NextAttemptAtUtc.Should().Be(now.AddSeconds(2));
        message.DeadLetteredAtUtc.Should().BeNull();

        message.MarkFailed("second", now, maxRetryAttempts: 3, TimeSpan.FromSeconds(3));
        message.RetryCount.Should().Be(2);
        message.NextAttemptAtUtc.Should().Be(now.AddSeconds(3));

        message.MarkFailed("third", now, maxRetryAttempts: 3, TimeSpan.FromSeconds(3));
        message.RetryCount.Should().Be(3);
        message.DeadLetteredAtUtc.Should().Be(now);
    }

    [Fact]
    public void Inbox_Should_Use_Integration_Event_Id_As_Idempotency_Key()
    {
        var eventId = Guid.NewGuid();
        var processedAt = new DateTime(2026, 7, 14, 15, 0, 0, DateTimeKind.Utc);

        var message = InboxMessage.CreateProcessed(
            eventId,
            "voting.vote_added",
            processedAt);

        message.Id.Should().Be(eventId);
        message.EventType.Should().Be("voting.vote_added");
        message.ReceivedAtUtc.Should().Be(processedAt);
        message.ProcessedAtUtc.Should().Be(processedAt);
    }

    private sealed class UnregisteredEvent : IEvent
    {
        public Guid EventId { get; } = Guid.NewGuid();
        public string EventType => "test.unregistered";
        public DateTime Timestamp { get; } = DateTime.UtcNow;
        public string Version => "1.0";
        public Guid CorrelationId { get; } = Guid.NewGuid();
        public Guid? CausationId => null;
    }
}
