using MediatR;
using Vitrin.Shared.Kernel.Results;

namespace Vitrin.Auth.Application.Commands;

public record LoginCommand(
    string Email,
    string Password) : IRequest<Result<string>>; // Returns JWT Token

public record LoginCommandHandler : IRequestHandler<LoginCommand, Result<string>>
{
    private static readonly string DummyPasswordHash =
        BCrypt.Net.BCrypt.HashPassword("vitrin-timing-protection-value");

    private readonly Interfaces.IUserRepository _userRepository;
    private readonly Interfaces.IJwtProvider _jwtProvider;

    public LoginCommandHandler(Interfaces.IUserRepository userRepository, Interfaces.IJwtProvider jwtProvider)
    {
        _userRepository = userRepository;
        _jwtProvider = jwtProvider;
    }

    public async Task<Result<string>> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByEmailAsync(request.Email, cancellationToken);

        var passwordHash = user is
            {
                Provider: Domain.Entities.AuthProvider.Local,
                PasswordHash: not null
            }
            ? user.PasswordHash
            : DummyPasswordHash;
        var passwordMatches = BCrypt.Net.BCrypt.Verify(request.Password, passwordHash);

        if (!passwordMatches || user is null || user.Provider != Domain.Entities.AuthProvider.Local)
            return Result<string>.Failure("E-posta veya şifre hatalı.");

        var token = _jwtProvider.Generate(user);
        return Result<string>.Success(token);
    }
}
