using System.Collections.Concurrent;
using System.Threading.Channels;
using Vitrin.Notification.Application.Notifications;

namespace Vitrin.Notification.Infrastructure.Realtime;

public sealed class NotificationStreamBroker : INotificationRealtimePublisher
{
    private readonly ConcurrentDictionary<Guid, ConcurrentDictionary<Guid, Channel<RealtimeNotification>>> _subscribers = new();

    public NotificationStreamSubscription Subscribe(Guid userId)
    {
        var subscriptionId = Guid.NewGuid();
        var channel = Channel.CreateBounded<RealtimeNotification>(new BoundedChannelOptions(100)
        {
            SingleReader = true,
            SingleWriter = false,
            FullMode = BoundedChannelFullMode.DropOldest
        });

        var userSubscriptions = _subscribers.GetOrAdd(userId, _ => new());
        userSubscriptions[subscriptionId] = channel;

        return new NotificationStreamSubscription(channel.Reader, () =>
        {
            if (!_subscribers.TryGetValue(userId, out var subscriptions)) return;
            if (subscriptions.TryRemove(subscriptionId, out var removed)) removed.Writer.TryComplete();
            if (subscriptions.IsEmpty) _subscribers.TryRemove(userId, out _);
        });
    }

    public ValueTask PublishAsync(RealtimeNotification notification, CancellationToken cancellationToken = default)
    {
        if (!_subscribers.TryGetValue(notification.UserId, out var subscriptions))
            return ValueTask.CompletedTask;

        foreach (var channel in subscriptions.Values)
            channel.Writer.TryWrite(notification);

        return ValueTask.CompletedTask;
    }
}

public sealed class NotificationStreamSubscription(
    ChannelReader<RealtimeNotification> reader,
    Action dispose) : IAsyncDisposable
{
    public ChannelReader<RealtimeNotification> Reader { get; } = reader;

    public ValueTask DisposeAsync()
    {
        dispose();
        return ValueTask.CompletedTask;
    }
}
