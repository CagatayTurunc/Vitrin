using Vitrin.Shared.Kernel.Domain;
using Vitrin.Shared.Kernel.Results;

namespace Vitrin.Notification.Domain.Entities;

public class NotificationItem : AggregateRoot
{
    public Guid UserId { get; private set; }
    public string Message { get; private set; } = string.Empty;
    public bool IsRead { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private NotificationItem() { }

    public static Result<NotificationItem> Create(Guid userId, string message)
    {
        if (string.IsNullOrWhiteSpace(message))
            return Result<NotificationItem>.Failure("Notification message cannot be empty.");

        var notification = new NotificationItem
        {
            UserId = userId,
            Message = message,
            IsRead = false,
            CreatedAt = DateTime.UtcNow
        };

        return Result<NotificationItem>.Success(notification);
    }

    public void MarkAsRead()
    {
        IsRead = true;
    }
}
