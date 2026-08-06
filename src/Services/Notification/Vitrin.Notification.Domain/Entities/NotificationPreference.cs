using Vitrin.Shared.Kernel.Domain;

namespace Vitrin.Notification.Domain.Entities;

public enum EmailDigestFrequency
{
    Off = 0,
    Daily = 1,
    Weekly = 2
}

public sealed class NotificationPreference : Entity
{
    public Guid UserId { get; private set; }
    public string? EmailAddress { get; private set; }
    public bool InAppEnabled { get; private set; } = true;
    public bool EmailEnabled { get; private set; }
    public EmailDigestFrequency DigestFrequency { get; private set; } = EmailDigestFrequency.Off;
    public bool ProductUpdatesEnabled { get; private set; } = true;
    public bool CommentsEnabled { get; private set; } = true;
    public bool MentionsEnabled { get; private set; } = true;
    public bool ReactionsEnabled { get; private set; } = true;
    public bool SocialEnabled { get; private set; } = true;
    public bool ModerationEnabled { get; private set; } = true;
    public DateTime? LastDigestSentAtUtc { get; private set; }
    public DateTime UpdatedAtUtc { get; private set; }

    private NotificationPreference() { }

    public static NotificationPreference CreateDefault(Guid userId, string? emailAddress = null)
    {
        return new NotificationPreference
        {
            UserId = userId,
            EmailAddress = NormalizeEmail(emailAddress),
            UpdatedAtUtc = DateTime.UtcNow
        };
    }

    public void Update(
        string? emailAddress,
        bool inAppEnabled,
        bool emailEnabled,
        EmailDigestFrequency digestFrequency,
        bool productUpdatesEnabled,
        bool commentsEnabled,
        bool mentionsEnabled,
        bool reactionsEnabled,
        bool socialEnabled,
        bool moderationEnabled,
        DateTime utcNow)
    {
        EmailAddress = NormalizeEmail(emailAddress);
        InAppEnabled = inAppEnabled;
        EmailEnabled = emailEnabled && EmailAddress is not null;
        DigestFrequency = EmailEnabled ? digestFrequency : EmailDigestFrequency.Off;
        ProductUpdatesEnabled = productUpdatesEnabled;
        CommentsEnabled = commentsEnabled;
        MentionsEnabled = mentionsEnabled;
        ReactionsEnabled = reactionsEnabled;
        SocialEnabled = socialEnabled;
        ModerationEnabled = moderationEnabled;
        UpdatedAtUtc = utcNow;
    }

    public bool AllowsType(string? notificationType)
    {
        var type = notificationType?.Trim().ToLowerInvariant() ?? "generic";
        if (type is "comment_mention") return MentionsEnabled;
        if (type is "comment_reaction") return ReactionsEnabled;
        if (type.StartsWith("comment", StringComparison.Ordinal)) return CommentsEnabled;
        if (type.StartsWith("product", StringComparison.Ordinal) ||
            type is "saved_search_match" or "topic_product_published") return ProductUpdatesEnabled;
        if (type is "follow" or "upvote" || type.StartsWith("social", StringComparison.Ordinal)) return SocialEnabled;
        if (type.StartsWith("account", StringComparison.Ordinal) ||
            type.StartsWith("appeal", StringComparison.Ordinal) ||
            type.StartsWith("maker", StringComparison.Ordinal)) return ModerationEnabled;
        return true;
    }

    public bool IsDigestDue(DateTime utcNow)
    {
        if (!EmailEnabled || DigestFrequency == EmailDigestFrequency.Off || EmailAddress is null) return false;
        if (LastDigestSentAtUtc is null) return true;

        var interval = DigestFrequency == EmailDigestFrequency.Weekly
            ? TimeSpan.FromDays(7)
            : TimeSpan.FromDays(1);
        return utcNow - LastDigestSentAtUtc.Value >= interval;
    }

    public DateTime DigestWindowStart(DateTime utcNow)
    {
        if (LastDigestSentAtUtc is { } lastSent) return lastSent;
        return DigestFrequency == EmailDigestFrequency.Weekly
            ? utcNow.AddDays(-7)
            : utcNow.AddDays(-1);
    }

    public void MarkDigestSent(DateTime sentAtUtc)
    {
        LastDigestSentAtUtc = sentAtUtc;
        UpdatedAtUtc = sentAtUtc;
    }

    private static string? NormalizeEmail(string? emailAddress)
    {
        var normalized = emailAddress?.Trim().ToLowerInvariant();
        return string.IsNullOrWhiteSpace(normalized) ? null : normalized;
    }
}
