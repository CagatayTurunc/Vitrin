using MediatR;
using Vitrin.Shared.Kernel.Results;

namespace Vitrin.Auth.Application.Commands;

public record LoginCommand(
    string Email,
    string Password) : IRequest<Result<string>>; // Returns JWT Token

public record LoginCommandHandler : IRequestHandler<LoginCommand, Result<string>>
{
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
        
        if (user is null || user.Provider != Domain.Entities.AuthProvider.Local)
            return Result<string>.Failure("Kullanıcı bulunamadı veya farklı bir yöntemle giriş yapılmış.");

        if (!BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            return Result<string>.Failure("Hatalı şifre.");

        var token = _jwtProvider.Generate(user);
        return Result<string>.Success(token);
    }
}
