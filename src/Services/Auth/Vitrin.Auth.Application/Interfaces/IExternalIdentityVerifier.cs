using Vitrin.Auth.Domain.Entities;
using Vitrin.Shared.Kernel.Results;

namespace Vitrin.Auth.Application.Interfaces;

public sealed record VerifiedExternalIdentity(
    string ProviderId,
    string Email,
    string FullName,
    string AvatarUrl);

public interface IExternalIdentityVerifier
{
    Task<Result<VerifiedExternalIdentity>> VerifyAsync(
        AuthProvider provider,
        string providerToken,
        CancellationToken cancellationToken = default);
}
