using Vitrin.Shared.Kernel.Domain;

namespace Vitrin.Product.Domain.Entities;

public sealed class SavedSearch : Entity
{
    public Guid UserId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string? Query { get; private set; }
    public string TopicSlugsCsv { get; private set; } = string.Empty;
    public int? MinUpvotes { get; private set; }
    public int? MinComments { get; private set; }
    public int? MinViews { get; private set; }
    public DateTime? PublishedFrom { get; private set; }
    public DateTime? PublishedTo { get; private set; }
    public string Sort { get; private set; } = "newest";
    public bool NotifyOnNewMatches { get; private set; } = true;
    public DateTime CreatedAtUtc { get; private set; }

    private SavedSearch() { }

    public static SavedSearch Create(
        Guid userId,
        string name,
        string? query,
        IEnumerable<string>? topicSlugs,
        int? minUpvotes,
        int? minComments,
        int? minViews,
        DateTime? publishedFrom,
        DateTime? publishedTo,
        string sort,
        bool notifyOnNewMatches,
        DateTime utcNow)
    {
        var normalizedTopics = (topicSlugs ?? Array.Empty<string>())
            .Select(value => value.Trim().ToLowerInvariant())
            .Where(value => value.Length > 0)
            .Distinct(StringComparer.Ordinal)
            .Take(20);

        return new SavedSearch
        {
            UserId = userId,
            Name = name.Trim(),
            Query = string.IsNullOrWhiteSpace(query) ? null : query.Trim(),
            TopicSlugsCsv = string.Join(',', normalizedTopics),
            MinUpvotes = minUpvotes,
            MinComments = minComments,
            MinViews = minViews,
            PublishedFrom = publishedFrom,
            PublishedTo = publishedTo,
            Sort = sort.Trim().ToLowerInvariant(),
            NotifyOnNewMatches = notifyOnNewMatches,
            CreatedAtUtc = utcNow
        };
    }

    public IReadOnlyList<string> GetTopicSlugs() => TopicSlugsCsv
        .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

    public bool Matches(ProductItem product)
    {
        if (Query is { Length: > 0 } query &&
            !product.Name.Contains(query, StringComparison.OrdinalIgnoreCase) &&
            !product.Tagline.Contains(query, StringComparison.OrdinalIgnoreCase) &&
            !product.Description.Contains(query, StringComparison.OrdinalIgnoreCase) &&
            !product.Topics.Any(topic => topic.Name.Contains(query, StringComparison.OrdinalIgnoreCase)))
            return false;

        var topics = GetTopicSlugs();
        if (topics.Count > 0 && !product.Topics.Any(topic => topics.Contains(topic.Slug))) return false;
        if (MinUpvotes is { } votes && product.Upvotes.Count < votes) return false;
        if (MinComments is { } comments && product.CommentCount < comments) return false;
        if (MinViews is { } views && product.ViewCount < views) return false;
        if (PublishedFrom is { } from && product.PublishedAt < from) return false;
        if (PublishedTo is { } to && product.PublishedAt > to) return false;
        return true;
    }
}

public sealed class TopicFollow : Entity
{
    public Guid UserId { get; private set; }
    public Guid TopicId { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }

    private TopicFollow() { }

    public static TopicFollow Create(Guid userId, Guid topicId, DateTime utcNow) => new()
    {
        UserId = userId,
        TopicId = topicId,
        CreatedAtUtc = utcNow
    };
}
