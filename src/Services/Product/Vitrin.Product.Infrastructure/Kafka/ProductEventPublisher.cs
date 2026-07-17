using Microsoft.Extensions.Logging;
using Vitrin.Product.Infrastructure.Data;
using Vitrin.Shared.Contracts.Events;
using Vitrin.Shared.Infrastructure.Outbox;

namespace Vitrin.Product.Infrastructure.Kafka;

public sealed class ProductEventPublisher(
    ProductDbContext dbContext,
    TimeProvider timeProvider,
    ILogger<ProductEventPublisher> logger)
{
    public async Task PublishProductPublished(
        ProductPublishedEvent @event,
        CancellationToken cancellationToken = default)
    {
        dbContext.OutboxMessages.Add(
            OutboxMessage.Create(@event, timeProvider.GetUtcNow().UtcDateTime));
        await dbContext.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Product mutation and outbox event committed atomically. EventId={EventId}, ProductId={ProductId}",
            @event.EventId,
            @event.ProductId);
    }

    /// <summary>
    /// ProductPublishedEvent'i outbox kuyruğuna ekler; caller SaveChangesAsync ile commit eder.
    /// </summary>
    public void EnqueueProductPublished(ProductPublishedEvent @event)
    {
        dbContext.OutboxMessages.Add(
            OutboxMessage.Create(@event, timeProvider.GetUtcNow().UtcDateTime));
    }

    public void EnqueueProductViewed(
        Guid productId,
        string productSlug,
        Guid? userId,
        string? ipAddress,
        string? userAgent,
        string? referrer)
    {
        var @event = new ProductViewedEvent
        {
            ProductId = productId,
            ProductSlug = productSlug,
            UserId = userId,
            IpAddress = ipAddress,
            UserAgent = userAgent,
            Referrer = referrer
        };

        dbContext.OutboxMessages.Add(
            OutboxMessage.Create(@event, timeProvider.GetUtcNow().UtcDateTime));
    }

    public void EnqueueSearchPerformed(string query, int resultCount, Guid? userId)
    {
        var @event = new SearchPerformedEvent
        {
            Query = query,
            ResultCount = resultCount,
            UserId = userId
        };

        dbContext.OutboxMessages.Add(
            OutboxMessage.Create(@event, timeProvider.GetUtcNow().UtcDateTime));
    }

    /// <summary>
    /// Ürün onaylandığında maker'a bildirim gönderir.
    /// Outbox kaydını ekler; caller SaveChangesAsync ile commit eder.
    /// </summary>
    public void EnqueueProductApprovedNotification(
        Guid makerId,
        string productName,
        Guid productId,
        DateTime? scheduledLaunchAt = null)
    {
        var message = scheduledLaunchAt is { } launchAt
            ? $"✅ \"{productName}\" ürününüz onaylandı ve {launchAt.ToLocalTime():dd.MM.yyyy HH:mm} tarihinde yayınlanacak."
            : $"🎉 \"{productName}\" ürününüz onaylandı ve artık yayında!";

        var notificationEvent = new SendNotificationEvent
        {
            RecipientUserId  = makerId,
            Message          = message,
            NotificationType = "product_approved",
            RelatedEntityId  = productId
        };

        dbContext.OutboxMessages.Add(
            OutboxMessage.Create(notificationEvent, timeProvider.GetUtcNow().UtcDateTime));
    }

    /// <summary>
    /// Ürün reddedildiğinde maker'a sebepli bildirim gönderir.
    /// Product state değişikliği ve outbox kaydı aynı SaveChanges çağrısında commit edilir.
    /// </summary>
    public void EnqueueProductRejectedNotification(
        Guid makerId,
        string productName,
        Guid productId,
        string reason)
    {
        var notificationEvent = new SendNotificationEvent
        {
            RecipientUserId  = makerId,
            Message          = $"❌ \"{productName}\" ürününüz reddedildi. Sebep: {reason}",
            NotificationType = "product_rejected",
            RelatedEntityId  = productId
        };

        dbContext.OutboxMessages.Add(
            OutboxMessage.Create(notificationEvent, timeProvider.GetUtcNow().UtcDateTime));
    }

    public void EnqueueScheduledProductPublishedNotification(Guid makerId, string productName, Guid productId)
    {
        var notificationEvent = new SendNotificationEvent
        {
            RecipientUserId = makerId,
            Message = $"🚀 \"{productName}\" planlandığı gibi yayına alındı!",
            NotificationType = "product_published",
            RelatedEntityId = productId
        };

        dbContext.OutboxMessages.Add(
            OutboxMessage.Create(notificationEvent, timeProvider.GetUtcNow().UtcDateTime));
    }

    public void EnqueueOwnershipNotification(
        Guid recipientUserId,
        string productName,
        Guid productId,
        bool approved,
        string? note = null)
    {
        var decision = approved ? "onaylandı; artık ürün sahibisiniz" : "reddedildi";
        var noteSuffix = string.IsNullOrWhiteSpace(note) ? string.Empty : $" Not: {note.Trim()}";
        var notificationEvent = new SendNotificationEvent
        {
            RecipientUserId = recipientUserId,
            Message = $"\"{productName}\" için sahiplik talebiniz {decision}.{noteSuffix}",
            NotificationType = approved ? "product_claim_approved" : "product_claim_rejected",
            RelatedEntityId = productId
        };

        dbContext.OutboxMessages.Add(
            OutboxMessage.Create(notificationEvent, timeProvider.GetUtcNow().UtcDateTime));
    }
}
