using Vitrin.Shared.Kernel.Results;

namespace Vitrin.Auth.Domain.ValueObjects;

public class Email
{
    public string Value { get; private set; } = null!;
    
    private Email() { } // EF Core
    
    private Email(string value)
    {
        Value = value;
    }
    
    public static Result<Email> Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return Result<Email>.Failure("Email cannot be empty");
        
        if (!IsValid(value))
            return Result<Email>.Failure("Invalid email format");
        
        return Result<Email>.Success(new Email(value.ToLowerInvariant()));
    }
    
    private static bool IsValid(string email)
    {
        try
        {
            var addr = new System.Net.Mail.MailAddress(email);
            return addr.Address == email;
        }
        catch
        {
            return false;
        }
    }
}
