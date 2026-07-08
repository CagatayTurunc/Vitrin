using Vitrin.Auth.Domain.ValueObjects;
using Vitrin.Shared.Contracts.Events;
using Vitrin.Shared.Kernel.Domain;
using Vitrin.Shared.Kernel.Results;

namespace Vitrin.Auth.Domain.Entities;

public class User : AggregateRoot
{
    public Email Email { get; private set; } = null!;
    public string? PasswordHash { get; private set; }
    public string FullName { get; private set; } = string.Empty;
    public string? AvatarUrl { get; private set; }
    public string? Bio { get; private set; }
    public Dictionary<string, string> SocialLinks { get; private set; } = new();
    public UserRole Role { get; private set; }
    public bool EmailVerified { get; private set; }
    public bool IsActive { get; private set; }
    public bool IsSuspended { get; private set; }
    public string? SuspensionReason { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? LastLoginAt { get; private set; }
    
    private User() { } // EF Core
    
    public static User Create(
        string email, 
        string? passwordHash, 
        string fullName, 
        string registrationMethod)
    {
        var user = new User
        {
            Email = Email.Create(email).Value,
            PasswordHash = passwordHash,
            FullName = fullName,
            Role = UserRole.User,
            EmailVerified = false,
            IsActive = true,
            IsSuspended = false,
            SocialLinks = new Dictionary<string, string>(),
            CreatedAt = DateTime.UtcNow
        };
        
        user.AddDomainEvent(new UserRegisteredEvent
        {
            UserId = user.Id,
            Email = email,
            FullName = fullName,
            RegistrationMethod = registrationMethod
        });
        
        return user;
    }
    
    public Result ChangeRole(UserRole newRole, Guid changedBy, string? reason = null)
    {
        if (Role == newRole)
            return Result.Failure("User already has this role");
        
        var oldRole = Role;
        Role = newRole;
        
        // Use standard C# record or just generic base event pattern
        // The event below is from Contracts.Events
        AddDomainEvent(new UserRoleChangedEvent
        {
            UserId = Id,
            OldRole = oldRole.ToString(),
            NewRole = newRole.ToString(),
            ChangedBy = changedBy,
            Reason = reason
        });
        
        return Result.Success();
    }
    
    public Result Suspend(string reason)
    {
        if (IsSuspended)
            return Result.Failure("User is already suspended");
        
        IsSuspended = true;
        IsActive = false;
        SuspensionReason = reason;
        
        // We can add a UserSuspendedEvent later
        return Result.Success();
    }
    
    public Result Activate()
    {
        if (!IsSuspended)
            return Result.Failure("User is not suspended");
        
        IsSuspended = false;
        IsActive = true;
        SuspensionReason = null;
        
        return Result.Success();
    }
    
    public void UpdateLastLogin()
    {
        LastLoginAt = DateTime.UtcNow;
    }
    
    public void VerifyEmail()
    {
        EmailVerified = true;
    }
}

public enum UserRole
{
    Guest = 0,
    User = 1,
    ProductOwner = 2,
    Moderator = 3,
    Admin = 4
}
