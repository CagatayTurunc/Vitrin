using FluentValidation;
using Vitrin.Auth.Application.Commands;

namespace Vitrin.Auth.Application.Validation;

public sealed class LoginCommandValidator : AbstractValidator<LoginCommand>
{
    public LoginCommandValidator()
    {
        RuleFor(command => command.Email)
            .NotEmpty()
            .EmailAddress()
            .MaximumLength(255);

        RuleFor(command => command.Password)
            .NotEmpty()
            .MaximumLength(128);
    }
}
