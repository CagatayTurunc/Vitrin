using Vitrin.Shared.Kernel.Domain;

namespace Vitrin.Auth.Domain.Entities;

public class User : AggregateRoot
{
    public string Email { get; private set; } = string.Empty;
    public string Username { get; private set; } = string.Empty;
    public string FullName { get; private set; } = string.Empty;
    public string AvatarUrl { get; private set; } = string.Empty;
    public string? Headline { get; private set; }
    public string? About { get; private set; }
    public string? WebsiteUrl { get; private set; }
    public string? GithubUrl { get; private set; }
    public string? LinkedInUrl { get; private set; }
    
    // Auth Providers
    public string? PasswordHash { get; private set; }
    public string? GoogleId { get; private set; }
    public string? GithubId { get; private set; }
    
    // Auth Provider Type
    public AuthProvider Provider { get; private set; }
    
    // User Role (RBAC)
    public UserRole Role { get; private set; }
    
    // Follow System
    private readonly List<UserFollow> _followers = new();
    public IReadOnlyCollection<UserFollow> Followers => _followers.AsReadOnly();
    
    private readonly List<UserFollow> _following = new();
    public IReadOnlyCollection<UserFollow> Following => _following.AsReadOnly();

    // Gamification & Streaks
    public int CurrentStreak { get; private set; }
    public int LongestStreak { get; private set; }
    public DateTime? LastVoteDate { get; private set; }

    private readonly List<UserBadge> _badges = new();
    public IReadOnlyCollection<UserBadge> Badges => _badges.AsReadOnly();

    public DateTime CreatedAt { get; private set; }
    public Guid? ActiveBanId { get; private set; }
    public DateTime? SuspendedUntilUtc { get; private set; }
    public string? SuspensionReason { get; private set; }

    // Constructor for EF Core
    protected User() { }

    private User(Guid id, string email, string username, string fullName, string avatarUrl, AuthProvider provider, string? passwordHash, string? googleId, string? githubId) 
        : base(id)
    {
        Email = email.Trim().ToLowerInvariant();
        Username = username.Trim().ToLowerInvariant();
        FullName = fullName.Trim();
        AvatarUrl = avatarUrl;
        Provider = provider;
        PasswordHash = passwordHash;
        GoogleId = googleId;
        GithubId = githubId;
        Role = UserRole.Member; // Default role
        CreatedAt = DateTime.UtcNow;
    }

    public void UpdateRole(UserRole newRole)
    {
        Role = newRole;
    }

    public void UpdateProfile(string fullName, string username, string? headline, string? about, string? avatarUrl, string? websiteUrl, string? githubUrl, string? linkedInUrl)
    {
        FullName = fullName.Trim();
        Username = username.Trim().ToLowerInvariant();
        Headline = headline;
        About = about;
        if (!string.IsNullOrEmpty(avatarUrl)) AvatarUrl = avatarUrl;
        WebsiteUrl = websiteUrl;
        GithubUrl = githubUrl;
        LinkedInUrl = linkedInUrl;
    }

    public static User CreateWithPassword(string email, string username, string fullName, string passwordHash)
    {
        return new User(Guid.NewGuid(), email, username, fullName, string.Empty, AuthProvider.Local, passwordHash, null, null);
    }

    public static User CreateWithGoogle(string email, string username, string fullName, string avatarUrl, string googleId)
    {
        return new User(Guid.NewGuid(), email, username, fullName, avatarUrl, AuthProvider.Google, null, googleId, null);
    }
    
    public static User CreateWithGithub(string email, string username, string fullName, string avatarUrl, string githubId)
    {
        return new User(Guid.NewGuid(), email, username, fullName, avatarUrl, AuthProvider.Github, null, null, githubId);
    }

    public void RecordVoteActivity()
    {
        var today = DateTime.UtcNow.Date;
        
        if (LastVoteDate.HasValue)
        {
            var diff = (today - LastVoteDate.Value.Date).TotalDays;

            if (diff == 1) // Voted yesterday, increment streak
            {
                CurrentStreak++;
                if (CurrentStreak > LongestStreak) LongestStreak = CurrentStreak;
            }
            else if (diff > 1) // Missed a day, reset streak
            {
                CurrentStreak = 1;
            }
            // If diff == 0, already voted today, do nothing to streak
        }
        else
        {
            // First time voting
            CurrentStreak = 1;
            if (CurrentStreak > LongestStreak) LongestStreak = CurrentStreak;
        }

        LastVoteDate = today;
    }

    public void AddBadge(string name, string icon)
    {
        if (!_badges.Any(b => b.Name == name))
        {
            _badges.Add(new UserBadge(Id, name, icon));
        }
    }

    public bool IsBanned(DateTime utcNow) =>
        ActiveBanId.HasValue && (!SuspendedUntilUtc.HasValue || SuspendedUntilUtc.Value > utcNow);

    public void Suspend(Guid banId, string reason, DateTime? untilUtc)
    {
        ActiveBanId = banId;
        SuspensionReason = reason.Trim();
        SuspendedUntilUtc = untilUtc;
    }

    public void LiftSuspension()
    {
        ActiveBanId = null;
        SuspensionReason = null;
        SuspendedUntilUtc = null;
    }
}

public enum AuthProvider
{
    Local = 0,
    Google = 1,
    Github = 2
}
