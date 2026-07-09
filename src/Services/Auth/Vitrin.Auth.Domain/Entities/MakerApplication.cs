using Vitrin.Shared.Kernel.Domain;

namespace Vitrin.Auth.Domain.Entities;

public enum ApplicationStatus
{
    Pending = 0,
    Approved = 1,
    Rejected = 2
}

public class MakerApplication : AggregateRoot
{
    public Guid UserId { get; private set; }
    public string PortfolioUrl { get; private set; } = string.Empty;
    public string Reason { get; private set; } = string.Empty;
    public ApplicationStatus Status { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? ProcessedAt { get; private set; }

    // EF Core
    protected MakerApplication() { }

    private MakerApplication(Guid id, Guid userId, string portfolioUrl, string reason) : base(id)
    {
        UserId = userId;
        PortfolioUrl = portfolioUrl;
        Reason = reason;
        Status = ApplicationStatus.Pending;
        CreatedAt = DateTime.UtcNow;
    }

    public static MakerApplication Create(Guid userId, string portfolioUrl, string reason)
    {
        return new MakerApplication(Guid.NewGuid(), userId, portfolioUrl, reason);
    }

    public void Approve()
    {
        Status = ApplicationStatus.Approved;
        ProcessedAt = DateTime.UtcNow;
    }

    public void Reject()
    {
        Status = ApplicationStatus.Rejected;
        ProcessedAt = DateTime.UtcNow;
    }
}
