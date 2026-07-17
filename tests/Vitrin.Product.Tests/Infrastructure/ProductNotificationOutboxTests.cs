using System.Text.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Vitrin.Product.Infrastructure.Data;
using Vitrin.Product.Infrastructure.Kafka;
using Vitrin.Shared.Contracts.Events;
using Xunit;

namespace Vitrin.Product.Tests.Infrastructure;

public sealed class ProductNotificationOutboxTests
{
    [Fact]
    public void Approval_Notification_Should_Target_Maker()
    {
        using var dbContext = CreateDbContext();
        var publisher = CreatePublisher(dbContext);
        var makerId = Guid.NewGuid();
        var productId = Guid.NewGuid();

        publisher.EnqueueProductApprovedNotification(makerId, "Vitrin Test", productId);

        var outboxMessage = dbContext.OutboxMessages.Local.Should().ContainSingle().Subject;
        var notification = JsonSerializer.Deserialize<SendNotificationEvent>(outboxMessage.Payload);

        outboxMessage.Topic.Should().Be(EventTopics.Notification);
        notification.Should().NotBeNull();
        notification!.RecipientUserId.Should().Be(makerId);
        notification.NotificationType.Should().Be("product_approved");
        notification.RelatedEntityId.Should().Be(productId);
        notification.Message.Should().Contain("onaylandı");
    }

    [Fact]
    public void Rejection_Notification_Should_Include_Reason()
    {
        using var dbContext = CreateDbContext();
        var publisher = CreatePublisher(dbContext);
        var makerId = Guid.NewGuid();
        var productId = Guid.NewGuid();

        publisher.EnqueueProductRejectedNotification(
            makerId,
            "Vitrin Test",
            productId,
            "Ekran görüntüleri eksik.");

        var outboxMessage = dbContext.OutboxMessages.Local.Should().ContainSingle().Subject;
        var notification = JsonSerializer.Deserialize<SendNotificationEvent>(outboxMessage.Payload);

        outboxMessage.Topic.Should().Be(EventTopics.Notification);
        notification.Should().NotBeNull();
        notification!.RecipientUserId.Should().Be(makerId);
        notification.NotificationType.Should().Be("product_rejected");
        notification.RelatedEntityId.Should().Be(productId);
        notification.Message.Should().Contain("Ekran görüntüleri eksik.");
    }

    [Fact]
    public void Product_View_Should_Be_Queued_For_Analytics()
    {
        using var dbContext = CreateDbContext();
        var publisher = CreatePublisher(dbContext);
        var productId = Guid.NewGuid();

        publisher.EnqueueProductViewed(
            productId,
            "vitrin-test",
            null,
            "127.0.0.1",
            "test-agent",
            null);

        var outboxMessage = dbContext.OutboxMessages.Local.Should().ContainSingle().Subject;
        var viewEvent = JsonSerializer.Deserialize<ProductViewedEvent>(outboxMessage.Payload);

        outboxMessage.Topic.Should().Be(EventTopics.Analytics);
        viewEvent.Should().NotBeNull();
        viewEvent!.ProductId.Should().Be(productId);
        viewEvent.ProductSlug.Should().Be("vitrin-test");
    }

    private static ProductDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<ProductDbContext>()
            .UseNpgsql("Host=localhost;Database=vitrin_test;Username=test;Password=test")
            .Options;

        return new ProductDbContext(options);
    }

    private static ProductEventPublisher CreatePublisher(ProductDbContext dbContext) =>
        new(dbContext, TimeProvider.System, NullLogger<ProductEventPublisher>.Instance);
}
