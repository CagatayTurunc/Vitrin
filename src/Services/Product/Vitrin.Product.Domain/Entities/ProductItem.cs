using Vitrin.Shared.Kernel.Domain;
using Vitrin.Shared.Kernel.Results;

namespace Vitrin.Product.Domain.Entities;

public class ProductItem : AggregateRoot
{
    public string Name { get; private set; } = string.Empty;
    public string Slug { get; private set; } = string.Empty;
    public string Tagline { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public string ThumbnailUrl { get; private set; } = string.Empty;
    public List<string> GalleryUrls { get; private set; } = new();
    
    public Guid MakerId { get; private set; }
    public ProductStatus Status { get; private set; }
    public string? RejectionReason { get; private set; }
    
    public DateTime CreatedAt { get; private set; }
    public DateTime? PublishedAt { get; private set; }
    public DateTime? ArchivedAt { get; private set; }
    public DateTime? ScheduledLaunchAt { get; private set; }
    public int ViewCount { get; private set; }
    public int CommentCount { get; private set; }
    
    private readonly List<ProductLink> _links = new();
    public IReadOnlyList<ProductLink> Links => _links.AsReadOnly();
    
    private readonly List<Topic> _topics = new();
    public IReadOnlyList<Topic> Topics => _topics.AsReadOnly();
    
    private readonly List<ProductUpvote> _upvotes = new();
    public IReadOnlyList<ProductUpvote> Upvotes => _upvotes.AsReadOnly();

    private readonly List<ProductTeamMember> _teamMembers = new();
    public IReadOnlyList<ProductTeamMember> TeamMembers => _teamMembers.AsReadOnly();

    private ProductItem() { } // EF Core
    
    public static ProductItem Create(Guid makerId, string name, string tagline, string description, string slug, string? thumbnailUrl = null)
    {
        var product = new ProductItem
        {
            MakerId = makerId,
            Name = name,
            Tagline = tagline,
            Description = description,
            Slug = slug,
            ThumbnailUrl = thumbnailUrl ?? string.Empty,
            Status = ProductStatus.Draft,
            CreatedAt = DateTime.UtcNow
        };
        
        // Add Domain Event if necessary (e.g., ProductCreatedEvent)
        return product;
    }

    public Result Publish()
    {
        if (Status == ProductStatus.Published)
            return Result.Failure("Product is already published.");
            
        Status = ProductStatus.Published;
        PublishedAt = DateTime.UtcNow;
        
        // Add ProductPublishedEvent
        return Result.Success();
    }
    
    public Result SubmitForReview()
    {
        if (Status != ProductStatus.Draft && Status != ProductStatus.Rejected)
            return Result.Failure("Only Draft or Rejected products can be submitted for review.");

        Status = ProductStatus.UnderReview;
        RejectionReason = null;
        return Result.Success();
    }
    
    public Result Approve(DateTime? utcNow = null)
    {
        if (Status != ProductStatus.UnderReview)
            return Result.Failure("Only products under review can be approved.");

        var now = utcNow ?? DateTime.UtcNow;
        Status = ScheduledLaunchAt is { } launchAt && launchAt > now
            ? ProductStatus.Scheduled
            : ProductStatus.Published;
        RejectionReason = null;
        PublishedAt = Status == ProductStatus.Published ? now : null;
        return Result.Success();
    }

    public Result SetScheduledLaunch(DateTime? scheduledLaunchAt, DateTime? utcNow = null)
    {
        if (Status is ProductStatus.Published or ProductStatus.Archived)
            return Result.Failure("Published or archived products cannot be scheduled.");

        if (Status == ProductStatus.Scheduled && scheduledLaunchAt is null)
            return Result.Failure("An approved scheduled launch cannot be cleared.");

        if (scheduledLaunchAt is { } requestedAt)
        {
            var normalized = requestedAt.Kind == DateTimeKind.Utc
                ? requestedAt
                : requestedAt.ToUniversalTime();
            if (normalized <= (utcNow ?? DateTime.UtcNow).AddMinutes(5))
                return Result.Failure("Scheduled launch must be at least 5 minutes in the future.");

            ScheduledLaunchAt = normalized;
        }
        else
        {
            ScheduledLaunchAt = null;
        }

        return Result.Success();
    }

    public Result PublishScheduled(DateTime? utcNow = null)
    {
        var now = utcNow ?? DateTime.UtcNow;
        if (Status != ProductStatus.Scheduled || ScheduledLaunchAt is null)
            return Result.Failure("Product is not scheduled for launch.");

        if (ScheduledLaunchAt > now)
            return Result.Failure("Scheduled launch time has not arrived yet.");

        Status = ProductStatus.Published;
        PublishedAt = now;
        return Result.Success();
    }
    
    public Result Reject(string? reason)
    {
        if (Status != ProductStatus.UnderReview)
            return Result.Failure("Only products under review can be rejected.");

        var normalizedReason = reason?.Trim();
        if (string.IsNullOrWhiteSpace(normalizedReason))
            return Result.Failure("A rejection reason is required.");

        if (normalizedReason.Length > 500)
            return Result.Failure("The rejection reason cannot exceed 500 characters.");

        Status = ProductStatus.Rejected;
        RejectionReason = normalizedReason;
        return Result.Success();
    }

    public Result Archive()
    {
        if (Status == ProductStatus.Archived)
            return Result.Failure("Product is already archived.");

        Status = ProductStatus.Archived;
        ArchivedAt = DateTime.UtcNow;
        return Result.Success();
    }

    /// <summary>
    /// Revert a rejected or under-review product back to Draft so the maker can edit and resubmit.
    /// </summary>
    public Result RetractToDraft()
    {
        if (Status != ProductStatus.UnderReview && Status != ProductStatus.Rejected)
            return Result.Failure("Only UnderReview or Rejected products can be retracted to Draft.");

        Status = ProductStatus.Draft;
        RejectionReason = null;
        return Result.Success();
    }

    public bool CanEdit(Guid userId) =>
        MakerId == userId || _teamMembers.Any(member =>
            member.UserId == userId && member.Role == ProductTeamRole.Editor);

    public bool CanViewManagement(Guid userId) =>
        MakerId == userId || _teamMembers.Any(member => member.UserId == userId);

    public Result AddOrUpdateTeamMember(Guid requestingUserId, Guid memberUserId, ProductTeamRole role)
    {
        if (MakerId != requestingUserId)
            return Result.Failure("Only the product owner can manage the team.");
        if (memberUserId == MakerId)
            return Result.Failure("The product owner is already on the team.");

        var existing = _teamMembers.FirstOrDefault(member => member.UserId == memberUserId);
        if (existing is null)
            _teamMembers.Add(ProductTeamMember.Create(Id, memberUserId, role));
        else
            existing.ChangeRole(role);

        return Result.Success();
    }

    public Result RemoveTeamMember(Guid requestingUserId, Guid memberUserId)
    {
        if (MakerId != requestingUserId)
            return Result.Failure("Only the product owner can manage the team.");

        var existing = _teamMembers.FirstOrDefault(member => member.UserId == memberUserId);
        if (existing is null)
            return Result.Failure("Team member not found.");

        _teamMembers.Remove(existing);
        return Result.Success();
    }

    public Result TransferOwnership(Guid requestingUserId, Guid newOwnerUserId)
    {
        if (MakerId != requestingUserId)
            return Result.Failure("Only the product owner can transfer ownership.");
        if (newOwnerUserId == MakerId)
            return Result.Failure("This user already owns the product.");

        var newOwnerMembership = _teamMembers.FirstOrDefault(member => member.UserId == newOwnerUserId);
        if (newOwnerMembership is null)
            return Result.Failure("The new owner must first be added to the product team.");

        var previousOwnerId = MakerId;
        _teamMembers.Remove(newOwnerMembership);
        MakerId = newOwnerUserId;

        if (_teamMembers.All(member => member.UserId != previousOwnerId))
            _teamMembers.Add(ProductTeamMember.Create(Id, previousOwnerId, ProductTeamRole.Editor));

        return Result.Success();
    }
    
    public void AddLink(string title, string url)
    {
        _links.Add(new ProductLink(Id, title, url));
    }
    
    public void AddTopic(Topic topic)
    {
        if (!_topics.Any(t => t.Id == topic.Id))
        {
            _topics.Add(topic);
        }
    }
    
    public void ToggleUpvote(Guid userId)
    {
        var existingUpvote = _upvotes.FirstOrDefault(u => u.UserId == userId);
        if (existingUpvote != null)
        {
            _upvotes.Remove(existingUpvote);
        }
        else
        {
            _upvotes.Add(new ProductUpvote(Id, userId));
        }
    }

    public void RecordView() => ViewCount++;

    public void RecordComment() => CommentCount++;
    
    public void SetGalleryUrls(IEnumerable<string> urls)
    {
        GalleryUrls.Clear();
        if (urls != null)
        {
            GalleryUrls.AddRange(urls);
        }
    }
}
