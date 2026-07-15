using Vitrin.Auth.Domain.Entities;

namespace Vitrin.Auth.Application.Interfaces;

public sealed class DuplicateIdentityException(string field, Exception innerException)
    : Exception($"A user with the same {field} already exists.", innerException)
{
    public string Field { get; } = field;
}

public interface IUserRepository
{
    Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);
    Task<User?> GetByUsernameAsync(string username, CancellationToken cancellationToken = default);
    Task<User?> GetByGoogleIdAsync(string googleId, CancellationToken cancellationToken = default);
    Task<User?> GetByGithubIdAsync(string githubId, CancellationToken cancellationToken = default);
    
    Task AddAsync(User user, CancellationToken cancellationToken = default);
    Task UpdateAsync(User user, CancellationToken cancellationToken = default);
}
