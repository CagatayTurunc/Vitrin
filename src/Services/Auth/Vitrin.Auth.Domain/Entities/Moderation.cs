using Vitrin.Shared.Kernel.Domain;
using Vitrin.Shared.Kernel.Results;

namespace Vitrin.Auth.Domain.Entities;

public enum ModerationTargetType
{
    Comment = 0,
    Product = 1,
    User = 2
}

public enum ReportCategory
{
    Spam = 0,
    Harassment = 1,
    Hate = 2,
    Misinformation = 3,
    Illegal = 4,
    Other = 5
}

public enum ModerationCaseStatus
{
    Open = 0,
    UnderReview = 1,
    Resolved = 2,
    Dismissed = 3
}

public sealed class ModerationReport : AggregateRoot
{
    public Guid ReporterUserId { get; private set; }
    public ModerationTargetType TargetType { get; private set; }
    public Guid TargetId { get; private set; }
    public Guid? TargetOwnerUserId { get; private set; }
    public ReportCategory Category { get; private set; }
    public string Details { get; private set; } = string.Empty;
    public ModerationCaseStatus Status { get; private set; }
    public Guid? ReviewedByUserId { get; private set; }
    public string? Resolution { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime? ReviewedAtUtc { get; private set; }

    private ModerationReport() { }

    public static Result<ModerationReport> Create(
        Guid reporterUserId,
        ModerationTargetType targetType,
        Guid targetId,
        Guid? targetOwnerUserId,
        ReportCategory category,
        string details)
    {
        if (reporterUserId == Guid.Empty || targetId == Guid.Empty)
            return Result<ModerationReport>.Failure("Reporter and target are required.");
        if (string.IsNullOrWhiteSpace(details) || details.Trim().Length < 10)
            return Result<ModerationReport>.Failure("Report details must contain at least 10 characters.");

        return Result<ModerationReport>.Success(new ModerationReport
        {
            ReporterUserId = reporterUserId,
            TargetType = targetType,
            TargetId = targetId,
            TargetOwnerUserId = targetOwnerUserId,
            Category = category,
            Details = details.Trim(),
            Status = ModerationCaseStatus.Open,
            CreatedAtUtc = DateTime.UtcNow
        });
    }

    public void Resolve(Guid moderatorUserId, string resolution, bool dismissed)
    {
        Status = dismissed ? ModerationCaseStatus.Dismissed : ModerationCaseStatus.Resolved;
        ReviewedByUserId = moderatorUserId;
        Resolution = resolution.Trim();
        ReviewedAtUtc = DateTime.UtcNow;
    }
}

public sealed class UserBan : AggregateRoot
{
    public Guid UserId { get; private set; }
    public Guid IssuedByUserId { get; private set; }
    public string Reason { get; private set; } = string.Empty;
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime? ExpiresAtUtc { get; private set; }
    public Guid? RevokedByUserId { get; private set; }
    public DateTime? RevokedAtUtc { get; private set; }
    public string? RevocationReason { get; private set; }

    private UserBan() { }

    public static UserBan Create(
        Guid userId,
        Guid issuedByUserId,
        string reason,
        DateTime? expiresAtUtc) => new()
    {
        UserId = userId,
        IssuedByUserId = issuedByUserId,
        Reason = reason.Trim(),
        CreatedAtUtc = DateTime.UtcNow,
        ExpiresAtUtc = expiresAtUtc
    };

    public bool IsActive(DateTime utcNow) =>
        !RevokedAtUtc.HasValue && (!ExpiresAtUtc.HasValue || ExpiresAtUtc.Value > utcNow);

    public void Revoke(Guid moderatorUserId, string reason)
    {
        if (RevokedAtUtc.HasValue) return;
        RevokedByUserId = moderatorUserId;
        RevokedAtUtc = DateTime.UtcNow;
        RevocationReason = reason.Trim();
    }
}

public enum AppealStatus
{
    Open = 0,
    Approved = 1,
    Rejected = 2
}

public sealed class ModerationAppeal : AggregateRoot
{
    public Guid BanId { get; private set; }
    public Guid UserId { get; private set; }
    public string Statement { get; private set; } = string.Empty;
    public AppealStatus Status { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public Guid? ReviewedByUserId { get; private set; }
    public string? ReviewNote { get; private set; }
    public DateTime? ReviewedAtUtc { get; private set; }

    private ModerationAppeal() { }

    public static Result<ModerationAppeal> Create(Guid banId, Guid userId, string statement)
    {
        if (string.IsNullOrWhiteSpace(statement) || statement.Trim().Length < 20)
            return Result<ModerationAppeal>.Failure("Appeal statement must contain at least 20 characters.");

        return Result<ModerationAppeal>.Success(new ModerationAppeal
        {
            BanId = banId,
            UserId = userId,
            Statement = statement.Trim(),
            Status = AppealStatus.Open,
            CreatedAtUtc = DateTime.UtcNow
        });
    }

    public void Review(Guid moderatorUserId, bool approved, string note)
    {
        Status = approved ? AppealStatus.Approved : AppealStatus.Rejected;
        ReviewedByUserId = moderatorUserId;
        ReviewNote = note.Trim();
        ReviewedAtUtc = DateTime.UtcNow;
    }
}

public sealed class ModerationAuditEntry : Entity
{
    public string Action { get; private set; } = string.Empty;
    public Guid? ActorUserId { get; private set; }
    public string ResourceType { get; private set; } = string.Empty;
    public string? ResourceId { get; private set; }
    public string Outcome { get; private set; } = string.Empty;
    public string? TraceId { get; private set; }
    public string? Details { get; private set; }
    public DateTime OccurredAtUtc { get; private set; }

    private ModerationAuditEntry() { }

    public static ModerationAuditEntry Create(
        string action,
        Guid? actorUserId,
        string resourceType,
        string? resourceId,
        string outcome,
        string? traceId,
        string? details,
        DateTime occurredAtUtc) => new()
    {
        Action = action,
        ActorUserId = actorUserId,
        ResourceType = resourceType,
        ResourceId = resourceId,
        Outcome = outcome,
        TraceId = traceId,
        Details = details,
        OccurredAtUtc = occurredAtUtc
    };
}
