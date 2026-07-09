using MediatR;
using Vitrin.Shared.Kernel.Results;
using Vitrin.Auth.Domain.Entities;

namespace Vitrin.Auth.Application.Commands;

public record ExternalLoginCommand(
    string Email,
    string FullName,
    string AvatarUrl,
    string ProviderId,
    AuthProvider Provider) : IRequest<Result<string>>; // Returns JWT Token

public record ExternalLoginCommandHandler : IRequestHandler<ExternalLoginCommand, Result<string>>
{
    private readonly Interfaces.IUserRepository _userRepository;
    private readonly Interfaces.IJwtProvider _jwtProvider;

    public ExternalLoginCommandHandler(Interfaces.IUserRepository userRepository, Interfaces.IJwtProvider jwtProvider)
    {
        _userRepository = userRepository;
        _jwtProvider = jwtProvider;
    }

    public async Task<Result<string>> Handle(ExternalLoginCommand request, CancellationToken cancellationToken)
    {
        User? user = null;

        if (request.Provider == AuthProvider.Google)
        {
            user = await _userRepository.GetByGoogleIdAsync(request.ProviderId, cancellationToken);
        }
        else if (request.Provider == AuthProvider.Github)
        {
            user = await _userRepository.GetByGithubIdAsync(request.ProviderId, cancellationToken);
        }

        // If user not found by ID, check by email (maybe they signed up with email before, then used Google)
        if (user is null)
        {
            user = await _userRepository.GetByEmailAsync(request.Email, cancellationToken);
            
            if (user is not null)
            {
                // In a real app, we would link the accounts here (UpdateAsync). For simplicity, we assume they are the same user.
                // We'd add the GoogleId to the existing user.
            }
            else
            {
                // Create a new user
                string baseUsername = request.Email.Split('@')[0].ToLowerInvariant().Replace(".", "");
                string uniqueUsername = baseUsername + Guid.NewGuid().ToString().Substring(0, 4);

                if (request.Provider == AuthProvider.Google)
                {
                    user = User.CreateWithGoogle(request.Email, uniqueUsername, request.FullName, request.AvatarUrl, request.ProviderId);
                }
                else
                {
                    user = User.CreateWithGithub(request.Email, uniqueUsername, request.FullName, request.AvatarUrl, request.ProviderId);
                }

                await _userRepository.AddAsync(user, cancellationToken);
            }
        }

        var token = _jwtProvider.Generate(user);
        return Result<string>.Success(token);
    }
}
