using MediatR;
using Vitrin.Shared.Kernel.Results;
using Vitrin.Auth.Domain.Entities;

namespace Vitrin.Auth.Application.Commands;

public record ExternalLoginCommand(
    AuthProvider Provider,
    string ProviderToken) : IRequest<Result<string>>; // Returns JWT Token

public record ExternalLoginCommandHandler : IRequestHandler<ExternalLoginCommand, Result<string>>
{
    private readonly Interfaces.IUserRepository _userRepository;
    private readonly Interfaces.IJwtProvider _jwtProvider;
    private readonly Interfaces.IExternalIdentityVerifier _identityVerifier;

    public ExternalLoginCommandHandler(
        Interfaces.IUserRepository userRepository,
        Interfaces.IJwtProvider jwtProvider,
        Interfaces.IExternalIdentityVerifier identityVerifier)
    {
        _userRepository = userRepository;
        _jwtProvider = jwtProvider;
        _identityVerifier = identityVerifier;
    }

    public async Task<Result<string>> Handle(ExternalLoginCommand request, CancellationToken cancellationToken)
    {
        var identityResult = await _identityVerifier.VerifyAsync(
            request.Provider,
            request.ProviderToken,
            cancellationToken);

        if (identityResult.IsFailure)
        {
            return Result<string>.Failure(identityResult.Error);
        }

        var identity = identityResult.Value;
        User? user;

        if (request.Provider == AuthProvider.Google)
        {
            user = await _userRepository.GetByGoogleIdAsync(identity.ProviderId, cancellationToken);
        }
        else
        {
            user = await _userRepository.GetByGithubIdAsync(identity.ProviderId, cancellationToken);
        }

        if (user is null)
        {
            var existingUser = await _userRepository.GetByEmailAsync(identity.Email, cancellationToken);
            
            if (existingUser is not null)
            {
                return Result<string>.Failure(
                    "This email is already registered with a different authentication method.");
            }

            var baseUsername = new string(identity.Email
                .Split('@')[0]
                .ToLowerInvariant()
                .Where(char.IsLetterOrDigit)
                .ToArray());
            if (string.IsNullOrWhiteSpace(baseUsername))
            {
                baseUsername = "user";
            }

            string uniqueUsername;
            do
            {
                uniqueUsername = $"{baseUsername}{Guid.NewGuid().ToString("N")[..8]}";
            }
            while (await _userRepository.GetByUsernameAsync(uniqueUsername, cancellationToken) is not null);

            user = request.Provider == AuthProvider.Google
                ? User.CreateWithGoogle(identity.Email, uniqueUsername, identity.FullName, identity.AvatarUrl, identity.ProviderId)
                : User.CreateWithGithub(identity.Email, uniqueUsername, identity.FullName, identity.AvatarUrl, identity.ProviderId);

            try
            {
                await _userRepository.AddAsync(user, cancellationToken);
            }
            catch (Interfaces.DuplicateIdentityException)
            {
                return Result<string>.Failure(
                    "This external identity or email was registered by another concurrent request.");
            }
        }

        var token = _jwtProvider.Generate(user);
        return Result<string>.Success(token);
    }
}
