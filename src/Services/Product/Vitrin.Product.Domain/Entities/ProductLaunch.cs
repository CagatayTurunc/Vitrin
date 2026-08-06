using Vitrin.Shared.Kernel.Domain;
using Vitrin.Shared.Kernel.Results;

namespace Vitrin.Product.Domain.Entities;

public enum ProductLaunchStatus
{
    Draft = 0,
    UnderReview = 1,
    Scheduled = 2,
    Published = 3,
    Rejected = 4,
    Archived = 5
}

/// <summary>
/// A time-bound release of a product. ProductItem is the permanent product page;
/// ProductLaunch represents the launch-day snapshot and ranking period.
/// </summary>
public sealed class ProductLaunch : Entity
{
    public Guid ProductId { get; private set; }
    public int SequenceNumber { get; private set; }
    public string VersionLabel { get; private set; } = string.Empty;
    public string Tagline { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public string ThumbnailUrl { get; private set; } = string.Empty;
    public List<string> GalleryUrls { get; private set; } = new();
    public ProductLaunchStatus Status { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime? ScheduledAtUtc { get; private set; }
    public DateTime? PublishedAtUtc { get; private set; }
    public DateTime? ArchivedAtUtc { get; private set; }
    public bool IsFeatured { get; private set; }
    public int? FinalRank { get; private set; }
    public double? FinalScore { get; private set; }

    private ProductLaunch() { }

    internal static ProductLaunch CreateInitial(
        Guid productId,
        string? versionLabel,
        string tagline,
        string description,
        string? thumbnailUrl,
        IEnumerable<string>? galleryUrls = null)
    {
        var normalizedVersionLabel = string.IsNullOrWhiteSpace(versionLabel)
            ? "İlk Lansman"
            : versionLabel.Trim();

        return new ProductLaunch
        {
            ProductId = productId,
            SequenceNumber = 1,
            VersionLabel = normalizedVersionLabel[..Math.Min(normalizedVersionLabel.Length, 80)],
            Tagline = tagline.Trim(),
            Description = description,
            ThumbnailUrl = thumbnailUrl ?? string.Empty,
            GalleryUrls = galleryUrls?.Distinct(StringComparer.Ordinal).Take(10).ToList() ?? new List<string>(),
            Status = ProductLaunchStatus.Draft,
            CreatedAtUtc = DateTime.UtcNow
        };
    }

    internal Result SubmitForReview()
    {
        if (Status is not (ProductLaunchStatus.Draft or ProductLaunchStatus.Rejected))
            return Result.Failure("Only draft or rejected launches can be submitted for review.");

        Status = ProductLaunchStatus.UnderReview;
        return Result.Success();
    }

    internal Result SetSchedule(DateTime? scheduledAtUtc)
    {
        ScheduledAtUtc = scheduledAtUtc;
        return Result.Success();
    }

    internal void Approve(DateTime nowUtc)
    {
        if (ScheduledAtUtc is { } scheduled && scheduled > nowUtc)
        {
            Status = ProductLaunchStatus.Scheduled;
            PublishedAtUtc = null;
            return;
        }

        Status = ProductLaunchStatus.Published;
        PublishedAtUtc = nowUtc;
    }

    internal void Publish(DateTime publishedAtUtc)
    {
        Status = ProductLaunchStatus.Published;
        PublishedAtUtc = publishedAtUtc;
    }

    internal void Reject() => Status = ProductLaunchStatus.Rejected;

    internal void Retract() => Status = ProductLaunchStatus.Draft;

    internal void Archive(DateTime archivedAtUtc)
    {
        Status = ProductLaunchStatus.Archived;
        ArchivedAtUtc = archivedAtUtc;
    }

    internal void SetGalleryUrls(IEnumerable<string>? galleryUrls)
    {
        GalleryUrls.Clear();
        if (galleryUrls is not null)
            GalleryUrls.AddRange(galleryUrls.Distinct(StringComparer.Ordinal).Take(10));
    }

    public void FinalizeRanking(int rank, double score)
    {
        if (rank < 1) throw new ArgumentOutOfRangeException(nameof(rank));
        FinalRank = rank;
        FinalScore = Math.Round(Math.Max(0, score), 3);
    }

    public void SetFeatured(bool featured) => IsFeatured = featured;
}
