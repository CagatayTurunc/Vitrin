using FluentValidation;
using Vitrin.Auth.Application.Commands;

namespace Vitrin.Auth.Application.Validation;

public sealed class RegisterCommandValidator : AbstractValidator<RegisterCommand>
{
    public RegisterCommandValidator()
    {
        RuleFor(command => command.Email)
            .NotEmpty()
            .EmailAddress()
            .MaximumLength(255);

        RuleFor(command => command.Username)
            .NotEmpty()
            .Length(3, 50)
            .Matches("^[a-zA-Z0-9_]+$")
            .WithMessage("Username may only contain letters, numbers, and underscores.");

        RuleFor(command => command.FullName)
            .NotEmpty()
            .Length(2, 100);

        RuleFor(command => command.Password)
            .NotEmpty()
            .Length(12, 128)
            .Matches("[A-Z]").WithMessage("Password must contain an uppercase letter.")
            .Matches("[a-z]").WithMessage("Password must contain a lowercase letter.")
            .Matches("[0-9]").WithMessage("Password must contain a number.")
            .Matches("[^a-zA-Z0-9]").WithMessage("Password must contain a special character.");
    }
}
