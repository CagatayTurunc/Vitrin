using FluentAssertions;
using Vitrin.Auth.Application.Commands;
using Vitrin.Auth.Application.Validation;
using Vitrin.Auth.Domain.Entities;
using Xunit;

namespace Vitrin.Auth.Tests.Application;

public sealed class AuthCommandValidatorTests
{
    [Fact]
    public async Task Register_rejects_weak_password_and_invalid_username()
    {
        var validator = new RegisterCommandValidator();
        var command = new RegisterCommand("person@example.com", "invalid user", "Test User", "weak");

        var result = await validator.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(error => error.PropertyName == nameof(command.Username));
        result.Errors.Should().Contain(error => error.PropertyName == nameof(command.Password));
    }

    [Fact]
    public async Task Register_accepts_well_formed_credentials()
    {
        var validator = new RegisterCommandValidator();
        var command = new RegisterCommand(
            "person@example.com",
            "test_user",
            "Test User",
            "StrongPassword!42");

        var result = await validator.ValidateAsync(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Login_rejects_invalid_email()
    {
        var validator = new LoginCommandValidator();

        var result = await validator.ValidateAsync(new LoginCommand("not-an-email", "password"));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(error => error.PropertyName == nameof(LoginCommand.Email));
    }

    [Theory]
    [InlineData(AuthProvider.Local)]
    [InlineData((AuthProvider)999)]
    public async Task External_login_rejects_unsupported_provider(AuthProvider provider)
    {
        var validator = new ExternalLoginCommandValidator();

        var result = await validator.ValidateAsync(new ExternalLoginCommand(provider, "provider-token"));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(error => error.PropertyName == nameof(ExternalLoginCommand.Provider));
    }

    [Theory]
    [InlineData(AuthProvider.Google)]
    [InlineData(AuthProvider.Github)]
    public async Task External_login_accepts_supported_provider(AuthProvider provider)
    {
        var validator = new ExternalLoginCommandValidator();

        var result = await validator.ValidateAsync(new ExternalLoginCommand(provider, "provider-token"));

        result.IsValid.Should().BeTrue();
    }
}
