using MediatR;
using Vitrin.Shared.Kernel.Results;

namespace Vitrin.Auth.Application.Commands;

public record RegisterCommand(
    string Email,
    string Username,
    string FullName,
    string Password) : IRequest<Result<string>>;

public record RegisterCommandHandler : IRequestHandler<RegisterCommand, Result<string>>
{
    private readonly Interfaces.IUserRepository _userRepository;
    private readonly Interfaces.IAccountActionTokenService _actionTokenService;
    private readonly Interfaces.IAccountEmailService _emailService;

    public RegisterCommandHandler(
        Interfaces.IUserRepository userRepository,
        Interfaces.IAccountActionTokenService actionTokenService,
        Interfaces.IAccountEmailService emailService)
    {
        _userRepository = userRepository;
        _actionTokenService = actionTokenService;
        _emailService = emailService;
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

        var confirmationToken = _actionTokenService.Generate(
            user,
            Interfaces.AccountActionPurpose.ConfirmEmail,
            TimeSpan.FromHours(24));
        await _emailService.SendEmailConfirmationAsync(user, confirmationToken, cancellationToken);

        return Result<string>.Success("Kaydın tamamlandı. E-posta adresini doğrulamak için gelen kutunu kontrol et.");
    }
}
