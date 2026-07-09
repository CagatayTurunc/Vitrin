using Vitrin.Shared.Kernel.Domain;

namespace Vitrin.Auth.Domain.Entities;

public class User : AggregateRoot
{
    public string Email { get; private set; }
    public string Username { get; private set; }
    public string FullName { get; private set; }
    public string AvatarUrl { get; private set; }
    
    // Auth Providers
    public string? PasswordHash { get; private set; }
    public string? GoogleId { get; private set; }
    public string? GithubId { get; private set; }
    
    // Auth Provider Type
    public AuthProvider Provider { get; private set; }
    
    // User Role (RBAC)
    public UserRole Role { get; private set; }
    
    public DateTime CreatedAt { get; private set; }

    // Constructor for EF Core
    protected User() { }

    private User(Guid id, string email, string username, string fullName, string avatarUrl, AuthProvider provider, string? passwordHash, string? googleId, string? githubId) 
        : base(id)
    {
        Email = email;
        Username = username;
        FullName = fullName;
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
}

public enum AuthProvider
{
    Local = 0,
    Google = 1,
    Github = 2
}
