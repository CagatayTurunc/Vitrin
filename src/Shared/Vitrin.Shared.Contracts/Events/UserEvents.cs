namespace Vitrin.Shared.Contracts.Events;

public class UserRegisteredEvent : BaseEvent
{
    public Guid UserId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string RegistrationMethod { get; set; } = string.Empty;
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    
    public UserRegisteredEvent() : base("user.registered") { }
}

public class UserRoleChangedEvent : BaseEvent
{
    public Guid UserId { get; set; }
    public string OldRole { get; set; } = string.Empty;
    public string NewRole { get; set; } = string.Empty;
    public Guid ChangedBy { get; set; }
    public string? Reason { get; set; }
    
    public UserRoleChangedEvent() : base("user.role_changed") { }
}
