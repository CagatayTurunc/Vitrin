using Vitrin.Shared.Kernel.Domain;

namespace Vitrin.Auth.Domain.Entities;

public class UserBadge : Entity
{
    public Guid UserId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string Icon { get; private set; } = string.Empty; // e.g. "Trophy", "Flame", "Star"
    public DateTime EarnedAt { get; private set; }

    public virtual User User { get; private set; } = null!;

    protected UserBadge() { }

    public UserBadge(Guid userId, string name, string icon) : base(Guid.NewGuid())
    {
        UserId = userId;
        Name = name;
        Icon = icon;
        EarnedAt = DateTime.UtcNow;
    }
}
