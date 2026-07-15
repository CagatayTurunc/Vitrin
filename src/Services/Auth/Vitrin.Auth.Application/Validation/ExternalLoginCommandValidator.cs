using FluentValidation;
using Vitrin.Auth.Application.Commands;
using Vitrin.Auth.Domain.Entities;

namespace Vitrin.Auth.Application.Validation;

public sealed class ExternalLoginCommandValidator : AbstractValidator<ExternalLoginCommand>
{
    public ExternalLoginCommandValidator()
    {
        RuleFor(command => command.Provider)
            .Must(provider => provider is AuthProvider.Google or AuthProvider.Github)
            .WithMessage("Only Google and GitHub external login providers are supported.");

        RuleFor(command => command.ProviderToken)
            .NotEmpty()
            .MaximumLength(16_384);
    }
}
