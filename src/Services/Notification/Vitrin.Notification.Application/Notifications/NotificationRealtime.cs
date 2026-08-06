using Vitrin.Notification.Domain.Entities;

namespace Vitrin.Notification.Application.Notifications;

public sealed record RealtimeNotification(
    Guid Id,
    Guid UserId,
    string Message,
    bool IsRead,
    DateTime CreatedAt,
    string? NotificationType,
    Guid? RelatedEntityId);

public interface INotificationRealtimePublisher
{
    ValueTask PublishAsync(RealtimeNotification notification, CancellationToken cancellationToken = default);
}

public static class RealtimeNotificationMapper
{
    public static RealtimeNotification From(NotificationItem notification) => new(
        notification.Id,
        notification.UserId,
        notification.Message,
        notification.IsRead,
        notification.CreatedAt,
        notification.NotificationType,
        notification.RelatedEntityId);
}
