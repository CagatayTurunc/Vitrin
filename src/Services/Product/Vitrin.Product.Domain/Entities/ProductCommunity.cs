using Vitrin.Shared.Kernel.Domain;
using Vitrin.Shared.Kernel.Results;

namespace Vitrin.Product.Domain.Entities;

public enum CommunityThreadCategory
{
    General = 0,
    Maker = 1,
    Technical = 2,
    Feedback = 3,
    Collaboration = 4,
    Support = 5,
    Changelog = 6
}

public enum CommunityThreadKind
{
    Discussion = 0,
    Question = 1,
    Feedback = 2,
    Poll = 3,
    Ama = 4,
    BuildInPublic = 5
}

public sealed class CommunityThread : AggregateRoot
{
    public Guid AuthorId { get; private set; }
    public Guid? ProductId { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string Slug { get; private set; } = string.Empty;
    public string Body { get; private set; } = string.Empty;
    public CommunityThreadCategory Category { get; private set; }
    public CommunityThreadKind Kind { get; private set; }
    public bool IsPinned { get; private set; }
    public bool IsLocked { get; private set; }
    public int ViewCount { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime UpdatedAtUtc { get; private set; }

    private CommunityThread() { }

    public static Result<CommunityThread> Create(
        Guid authorId,
        Guid? productId,
        string title,
        string slug,
        string body,
        CommunityThreadCategory category,
        CommunityThreadKind kind)
    {
        title = title.Trim();
        slug = slug.Trim();
        body = body.Trim();
        if (title.Length is < 5 or > 160) return Result<CommunityThread>.Failure("Title must be between 5 and 160 characters.");
        if (slug.Length is < 3 or > 180) return Result<CommunityThread>.Failure("Slug is invalid.");
        if (body.Length is < 10 or > 20_000) return Result<CommunityThread>.Failure("Body must be between 10 and 20000 characters.");

        var now = DateTime.UtcNow;
        return Result<CommunityThread>.Success(new CommunityThread
        {
            AuthorId = authorId,
            ProductId = productId,
            Title = title,
            Slug = slug,
            Body = body,
            Category = category,
            Kind = kind,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        });
    }

    public void RecordView() => ViewCount++;
    public void SetModeration(bool isPinned, bool isLocked)
    {
        IsPinned = isPinned;
        IsLocked = isLocked;
        UpdatedAtUtc = DateTime.UtcNow;
    }
}

public sealed class CommunityReply : Entity
{
    public Guid ThreadId { get; private set; }
    public Guid AuthorId { get; private set; }
    public Guid? ParentReplyId { get; private set; }
    public string Body { get; private set; } = string.Empty;
    public bool IsOfficial { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime? EditedAtUtc { get; private set; }

    private CommunityReply() { }

    public static Result<CommunityReply> Create(Guid threadId, Guid authorId, Guid? parentReplyId, string body, bool isOfficial)
    {
        body = body.Trim();
        if (body.Length is < 2 or > 10_000) return Result<CommunityReply>.Failure("Reply must be between 2 and 10000 characters.");
        return Result<CommunityReply>.Success(new CommunityReply
        {
            ThreadId = threadId,
            AuthorId = authorId,
            ParentReplyId = parentReplyId,
            Body = body,
            IsOfficial = isOfficial,
            CreatedAtUtc = DateTime.UtcNow
        });
    }
}

public sealed class CommunityReaction : Entity
{
    public Guid UserId { get; private set; }
    public Guid? ThreadId { get; private set; }
    public Guid? ReplyId { get; private set; }
    public string Type { get; private set; } = "like";
    public DateTime CreatedAtUtc { get; private set; }
    private CommunityReaction() { }
    public static CommunityReaction ForThread(Guid userId, Guid threadId) => new() { UserId = userId, ThreadId = threadId, CreatedAtUtc = DateTime.UtcNow };
    public static CommunityReaction ForReply(Guid userId, Guid replyId) => new() { UserId = userId, ReplyId = replyId, CreatedAtUtc = DateTime.UtcNow };
}

public sealed class CommunityThreadFollow : Entity
{
    public Guid UserId { get; private set; }
    public Guid ThreadId { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    private CommunityThreadFollow() { }
    public static CommunityThreadFollow Create(Guid userId, Guid threadId) => new() { UserId = userId, ThreadId = threadId, CreatedAtUtc = DateTime.UtcNow };
}

public sealed class CommunityReport : Entity
{
    public Guid ReporterId { get; private set; }
    public Guid ThreadId { get; private set; }
    public string Reason { get; private set; } = string.Empty;
    public DateTime CreatedAtUtc { get; private set; }
    private CommunityReport() { }
    public static CommunityReport Create(Guid reporterId, Guid threadId, string reason) => new()
    {
        ReporterId = reporterId,
        ThreadId = threadId,
        Reason = reason.Trim()[..Math.Min(reason.Trim().Length, 500)],
        CreatedAtUtc = DateTime.UtcNow
    };
}

public enum ProductUsageStatus { Using = 0, UsedBefore = 1, Trial = 2 }

public sealed class ProductReview : Entity
{
    public Guid ProductId { get; private set; }
    public Guid UserId { get; private set; }
    public int Rating { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string Body { get; private set; } = string.Empty;
    public ProductUsageStatus UsageStatus { get; private set; }
    public bool IsVerified { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime UpdatedAtUtc { get; private set; }
    private ProductReview() { }

    public static Result<ProductReview> Create(Guid productId, Guid userId, int rating, string title, string body, ProductUsageStatus usageStatus)
    {
        if (rating is < 1 or > 5) return Result<ProductReview>.Failure("Rating must be between 1 and 5.");
        title = title.Trim(); body = body.Trim();
        if (title.Length is < 3 or > 120) return Result<ProductReview>.Failure("Review title must be between 3 and 120 characters.");
        if (body.Length is < 10 or > 5000) return Result<ProductReview>.Failure("Review body must be between 10 and 5000 characters.");
        var now = DateTime.UtcNow;
        return Result<ProductReview>.Success(new ProductReview { ProductId = productId, UserId = userId, Rating = rating, Title = title, Body = body, UsageStatus = usageStatus, CreatedAtUtc = now, UpdatedAtUtc = now });
    }

    public Result Update(int rating, string title, string body, ProductUsageStatus usageStatus)
    {
        if (rating is < 1 or > 5) return Result.Failure("Rating must be between 1 and 5.");
        title = title.Trim(); body = body.Trim();
        if (title.Length is < 3 or > 120 || body.Length is < 10 or > 5000) return Result.Failure("Review content is invalid.");
        Rating = rating; Title = title; Body = body; UsageStatus = usageStatus; UpdatedAtUtc = DateTime.UtcNow;
        return Result.Success();
    }
}

public sealed class ProductReviewHelpful : Entity
{
    public Guid ReviewId { get; private set; }
    public Guid UserId { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    private ProductReviewHelpful() { }
    public static ProductReviewHelpful Create(Guid reviewId, Guid userId) => new() { ReviewId = reviewId, UserId = userId, CreatedAtUtc = DateTime.UtcNow };
}

public sealed class ProductChangelogEntry : Entity
{
    public Guid ProductId { get; private set; }
    public Guid AuthorId { get; private set; }
    public string Version { get; private set; } = string.Empty;
    public string Title { get; private set; } = string.Empty;
    public string Body { get; private set; } = string.Empty;
    public DateTime PublishedAtUtc { get; private set; }
    private ProductChangelogEntry() { }

    public static Result<ProductChangelogEntry> Create(Guid productId, Guid authorId, string version, string title, string body)
    {
        version = version.Trim(); title = title.Trim(); body = body.Trim();
        if (version.Length is < 1 or > 40 || title.Length is < 3 or > 140 || body.Length is < 10 or > 10_000)
            return Result<ProductChangelogEntry>.Failure("Changelog content is invalid.");
        return Result<ProductChangelogEntry>.Success(new ProductChangelogEntry { ProductId = productId, AuthorId = authorId, Version = version, Title = title, Body = body, PublishedAtUtc = DateTime.UtcNow });
    }
}

public sealed class CollectionFollow : Entity
{
    public Guid CollectionId { get; private set; }
    public Guid UserId { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    private CollectionFollow() { }
    public static CollectionFollow Create(Guid collectionId, Guid userId) => new() { CollectionId = collectionId, UserId = userId, CreatedAtUtc = DateTime.UtcNow };
}

public enum DiscoverySignalKind { Hidden = 0, NotInterested = 1 }

public sealed class ProductDiscoverySignal : Entity
{
    public Guid UserId { get; private set; }
    public Guid ProductId { get; private set; }
    public DiscoverySignalKind Kind { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    private ProductDiscoverySignal() { }
    public static ProductDiscoverySignal Create(Guid userId, Guid productId, DiscoverySignalKind kind) => new()
    {
        UserId = userId,
        ProductId = productId,
        Kind = kind,
        CreatedAtUtc = DateTime.UtcNow
    };
    public void ChangeKind(DiscoverySignalKind kind) => Kind = kind;
}

public sealed class ProductMarketProfile : Entity
{
    public Guid ProductId { get; private set; }
    public string PricingModel { get; private set; } = "unspecified";
    public string PlatformsCsv { get; private set; } = "web";
    public bool HasFreePlan { get; private set; }
    public bool HasTurkishSupport { get; private set; }
    public bool SupportsTryPricing { get; private set; }
    public bool IsKvkkCompliant { get; private set; }
    public bool SupportsEFatura { get; private set; }
    public bool SupportsLocalPayments { get; private set; }
    public bool IsOpenSource { get; private set; }
    public DateTime UpdatedAtUtc { get; private set; }
    private ProductMarketProfile() { }
    public static ProductMarketProfile Create(Guid productId) => new() { ProductId = productId, UpdatedAtUtc = DateTime.UtcNow };
    public void Update(string pricingModel, IEnumerable<string>? platforms, bool hasFreePlan, bool hasTurkishSupport,
        bool supportsTryPricing, bool isKvkkCompliant, bool supportsEFatura, bool supportsLocalPayments, bool isOpenSource)
    {
        PricingModel = string.IsNullOrWhiteSpace(pricingModel) ? "unspecified" : pricingModel.Trim().ToLowerInvariant()[..Math.Min(pricingModel.Trim().Length, 30)];
        PlatformsCsv = string.Join(',', (platforms ?? Array.Empty<string>()).Select(value => value.Trim().ToLowerInvariant()).Where(value => value.Length > 0).Distinct().Take(8));
        HasFreePlan = hasFreePlan;
        HasTurkishSupport = hasTurkishSupport;
        SupportsTryPricing = supportsTryPricing;
        IsKvkkCompliant = isKvkkCompliant;
        SupportsEFatura = supportsEFatura;
        SupportsLocalPayments = supportsLocalPayments;
        IsOpenSource = isOpenSource;
        UpdatedAtUtc = DateTime.UtcNow;
    }
    public IReadOnlyList<string> GetPlatforms() => PlatformsCsv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
}

public enum DiscoverySignalKind { Hidden = 0, NotInterested = 1 }

public sealed class ProductDiscoverySignal : Entity
{
    public Guid UserId { get; private set; }
    public Guid ProductId { get; private set; }
    public DiscoverySignalKind Kind { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    private ProductDiscoverySignal() { }
    public static ProductDiscoverySignal Create(Guid userId, Guid productId, DiscoverySignalKind kind) => new()
    {
        UserId = userId,
        ProductId = productId,
        Kind = kind,
        CreatedAtUtc = DateTime.UtcNow
    };
    public void ChangeKind(DiscoverySignalKind kind) => Kind = kind;
}

public sealed class ProductMarketProfile : Entity
{
    public Guid ProductId { get; private set; }
    public string PricingModel { get; private set; } = "unspecified";
    public string PlatformsCsv { get; private set; } = "web";
    public bool HasFreePlan { get; private set; }
    public bool HasTurkishSupport { get; private set; }
    public bool SupportsTryPricing { get; private set; }
    public bool IsKvkkCompliant { get; private set; }
    public bool SupportsEFatura { get; private set; }
    public bool SupportsLocalPayments { get; private set; }
    public bool IsOpenSource { get; private set; }
    public DateTime UpdatedAtUtc { get; private set; }
    private ProductMarketProfile() { }
    public static ProductMarketProfile Create(Guid productId) => new() { ProductId = productId, UpdatedAtUtc = DateTime.UtcNow };
    public void Update(string pricingModel, IEnumerable<string>? platforms, bool hasFreePlan, bool hasTurkishSupport,
        bool supportsTryPricing, bool isKvkkCompliant, bool supportsEFatura, bool supportsLocalPayments, bool isOpenSource)
    {
        PricingModel = string.IsNullOrWhiteSpace(pricingModel) ? "unspecified" : pricingModel.Trim().ToLowerInvariant()[..Math.Min(pricingModel.Trim().Length, 30)];
        PlatformsCsv = string.Join(',', (platforms ?? Array.Empty<string>()).Select(value => value.Trim().ToLowerInvariant()).Where(value => value.Length > 0).Distinct().Take(8));
        HasFreePlan = hasFreePlan;
        HasTurkishSupport = hasTurkishSupport;
        SupportsTryPricing = supportsTryPricing;
        IsKvkkCompliant = isKvkkCompliant;
        SupportsEFatura = supportsEFatura;
        SupportsLocalPayments = supportsLocalPayments;
        IsOpenSource = isOpenSource;
        UpdatedAtUtc = DateTime.UtcNow;
    }
    public IReadOnlyList<string> GetPlatforms() => PlatformsCsv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
}
