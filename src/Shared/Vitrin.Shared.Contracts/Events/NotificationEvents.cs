namespace Vitrin.Shared.Contracts.Events;

/// <summary>
/// Herhangi bir servisin bildirim göndermesi gerektiğinde
/// "notification-events" topic'ine publish ettiği event.
/// Notification servisi consume edip SQLite'a kaydeder.
/// </summary>
public class SendNotificationEvent : BaseEvent
{
    /// <summary>Bildirimi alacak kullanıcının Id'si.</summary>
    public Guid RecipientUserId { get; set; }

    /// <summary>Bildirim mesajı.</summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>Kaynağı tanımlamak için (opsiyonel): "upvote", "comment", "follow", "maker_approved"</summary>
    public string? NotificationType { get; set; }

    /// <summary>İlgili kaynak Id'si (ürün, yorum vs.) — opsiyonel.</summary>
    public Guid? RelatedEntityId { get; set; }

    public SendNotificationEvent() : base("notification.send") { }
}
