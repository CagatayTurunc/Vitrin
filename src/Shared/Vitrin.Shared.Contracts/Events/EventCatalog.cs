namespace Vitrin.Shared.Contracts.Events;

public static class EventTopics
{
    public const string Voting = "voting-events";
    public const string Notification = "notification-events";
    public const string Analytics = "analytics-events";
    public const string Social = "social-events";
    public const string User = "user-events";
}

public static class EventCatalog
{
    private static readonly IReadOnlyDictionary<Type, string> Topics =
        new Dictionary<Type, string>
        {
            [typeof(VoteAddedEvent)] = EventTopics.Voting,
            [typeof(VoteRemovedEvent)] = EventTopics.Voting,
            [typeof(SendNotificationEvent)] = EventTopics.Notification,
            [typeof(ProductViewedEvent)] = EventTopics.Analytics,
            [typeof(ProductUpvotedEvent)] = EventTopics.Analytics,
            [typeof(SearchPerformedEvent)] = EventTopics.Analytics,
            [typeof(CommentCreatedAnalyticsEvent)] = EventTopics.Analytics,
            [typeof(ProductPublishedEvent)] = EventTopics.Social,
            [typeof(CommentAddedEvent)] = EventTopics.Social,
            [typeof(CommentRepliedEvent)] = EventTopics.Social,
            [typeof(UserRegisteredEvent)] = EventTopics.User,
            [typeof(UserRoleChangedEvent)] = EventTopics.User
        };

    public static IReadOnlyDictionary<Type, string> Entries => Topics;

    public static string GetTopic<TEvent>() where TEvent : IEvent =>
        GetTopic(typeof(TEvent));

    public static string GetTopic(IEvent @event) => GetTopic(@event.GetType());

    public static string GetTopic(Type eventType)
    {
        if (Topics.TryGetValue(eventType, out var topic))
        {
            return topic;
        }

        throw new InvalidOperationException(
            $"Event type '{eventType.FullName}' is not registered in the event catalog.");
    }
}
