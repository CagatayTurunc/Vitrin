using Vitrin.Shared.Kernel.Domain;
using Vitrin.Shared.Kernel.Results;

namespace Vitrin.Product.Domain.Entities;

public enum ProductTeamRole
{
    Viewer = 0,
    Editor = 1
}

public sealed class ProductTeamMember : Entity
{
    public Guid ProductId { get; private set; }
    public Guid UserId { get; private set; }
    public ProductTeamRole Role { get; private set; }
    public DateTime AddedAt { get; private set; }

    private ProductTeamMember() { }

    internal static ProductTeamMember Create(Guid productId, Guid userId, ProductTeamRole role) => new()
    {
        ProductId = productId,
        UserId = userId,
        Role = role,
        AddedAt = DateTime.UtcNow
    };

    internal void ChangeRole(ProductTeamRole role) => Role = role;
}

public enum ProductClaimStatus
{
    Pending = 0,
    Approved = 1,
    Rejected = 2
}

public sealed class ProductClaimRequest : Entity
{
    public Guid ProductId { get; private set; }
    public Guid ClaimantUserId { get; private set; }
    public string ClaimantUsername { get; private set; } = string.Empty;
    public string Message { get; private set; } = string.Empty;
    public ProductClaimStatus Status { get; private set; }
    public string? ReviewNote { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? ReviewedAt { get; private set; }
    public Guid? ReviewedByUserId { get; private set; }

    private ProductClaimRequest() { }

    public static Result<ProductClaimRequest> Create(
        Guid productId,
        Guid claimantUserId,
        string claimantUsername,
        string message)
    {
        var normalizedMessage = message.Trim();
        if (normalizedMessage.Length < 10)
            return Result<ProductClaimRequest>.Failure("Ownership claim message must be at least 10 characters.");
        if (normalizedMessage.Length > 1000)
            return Result<ProductClaimRequest>.Failure("Ownership claim message cannot exceed 1000 characters.");

        return Result<ProductClaimRequest>.Success(new ProductClaimRequest
        {
            ProductId = productId,
            ClaimantUserId = claimantUserId,
            ClaimantUsername = claimantUsername.Trim(),
            Message = normalizedMessage,
            Status = ProductClaimStatus.Pending,
            CreatedAt = DateTime.UtcNow
        });
    }

    public Result Approve(Guid reviewerUserId, string? note)
    {
        if (Status != ProductClaimStatus.Pending)
            return Result.Failure("Only pending claims can be approved.");

        Status = ProductClaimStatus.Approved;
        ReviewNote = NormalizeNote(note);
        ReviewedByUserId = reviewerUserId;
        ReviewedAt = DateTime.UtcNow;
        return Result.Success();
    }

    public Result Reject(Guid reviewerUserId, string? note)
    {
        if (Status != ProductClaimStatus.Pending)
            return Result.Failure("Only pending claims can be rejected.");

        Status = ProductClaimStatus.Rejected;
        ReviewNote = NormalizeNote(note);
        ReviewedByUserId = reviewerUserId;
        ReviewedAt = DateTime.UtcNow;
        return Result.Success();
    }

    private static string? NormalizeNote(string? note)
    {
        var normalized = note?.Trim();
        return string.IsNullOrWhiteSpace(normalized) ? null : normalized[..Math.Min(500, normalized.Length)];
    }
}

public sealed class ProductRevision : Entity
{
    public Guid ProductId { get; private set; }
    public int RevisionNumber { get; private set; }
    public Guid ChangedByUserId { get; private set; }
    public string ChangedByUsername { get; private set; } = string.Empty;
    public string ChangeType { get; private set; } = string.Empty;
    public string? Summary { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string Tagline { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public string ThumbnailUrl { get; private set; } = string.Empty;
    public List<string> GalleryUrls { get; private set; } = new();
    public ProductStatus Status { get; private set; }
    public DateTime? ScheduledLaunchAt { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private ProductRevision() { }

    public static ProductRevision Create(
        ProductItem product,
        int revisionNumber,
        Guid changedByUserId,
        string changedByUsername,
        string changeType,
        string? summary = null) => new()
    {
        ProductId = product.Id,
        RevisionNumber = revisionNumber,
        ChangedByUserId = changedByUserId,
        ChangedByUsername = changedByUsername.Trim(),
        ChangeType = changeType.Trim(),
        Summary = string.IsNullOrWhiteSpace(summary) ? null : summary.Trim(),
        Name = product.Name,
        Tagline = product.Tagline,
        Description = product.Description,
        ThumbnailUrl = product.ThumbnailUrl,
        GalleryUrls = [.. product.GalleryUrls],
        Status = product.Status,
        ScheduledLaunchAt = product.ScheduledLaunchAt,
        CreatedAt = DateTime.UtcNow
    };
}
