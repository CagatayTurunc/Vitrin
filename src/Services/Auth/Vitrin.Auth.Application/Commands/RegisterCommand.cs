using MediatR;
using Vitrin.Shared.Kernel.Results;

namespace Vitrin.Auth.Application.Commands;

public record RegisterCommand(
    string Email,
    string Username,
    string FullName,
    string Password) : IRequest<Result<string>>; // Returns JWT Token

public record RegisterCommandHandler : IRequestHandler<RegisterCommand, Result<string>>
{
    private readonly Interfaces.IUserRepository _userRepository;
    private readonly Interfaces.IJwtProvider _jwtProvider;

    public RegisterCommandHandler(Interfaces.IUserRepository userRepository, Interfaces.IJwtProvider jwtProvider)
    {
        _userRepository = userRepository;
        _jwtProvider = jwtProvider;
    }

    public async Task<Result<string>> Handle(RegisterCommand request, CancellationToken cancellationToken)
    {
        var existingEmail = await _userRepository.GetByEmailAsync(request.Email, cancellationToken);
        if (existingEmail is not null)
            return Result<string>.Failure("Bu e-posta adresi zaten kullanımda.");

        var existingUsername = await _userRepository.GetByUsernameAsync(request.Username, cancellationToken);
        if (existingUsername is not null)
            return Result<string>.Failure("Bu kullanıcı adı zaten kullanımda.");

        var passwordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);
        
        var user = Domain.Entities.User.CreateWithPassword(request.Email, request.Username, request.FullName, passwordHash);

        try
        {
            await _userRepository.AddAsync(user, cancellationToken);
        }
        catch (Interfaces.DuplicateIdentityException)
        {
            return Result<string>.Failure("E-posta adresi veya kullanıcı adı eşzamanlı başka bir istek tarafından alındı.");
        }

        var token = _jwtProvider.Generate(user);
        return Result<string>.Success(token);
    }
}
