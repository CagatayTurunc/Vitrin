using FluentAssertions;
using Moq;
using Vitrin.Auth.Application.Commands;
using Vitrin.Auth.Application.Interfaces;
using Vitrin.Auth.Domain.Entities;
using Vitrin.Shared.Kernel.Results;
using Xunit;

namespace Vitrin.Auth.Tests.Application;

public class ExternalLoginCommandHandlerTests
{
    private readonly Mock<IUserRepository> _repository = new();
    private readonly Mock<IJwtProvider> _jwtProvider = new();
    private readonly Mock<IExternalIdentityVerifier> _identityVerifier = new();

    [Fact]
    public async Task Handle_WhenProviderTokenCannotBeVerified_ShouldRejectLogin()
    {
        var command = new ExternalLoginCommand(AuthProvider.Google, "forged-token");
        _identityVerifier
            .Setup(verifier => verifier.VerifyAsync(
                AuthProvider.Google,
                command.ProviderToken,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<VerifiedExternalIdentity>.Failure("verification failed"));

        var result = await CreateHandler().Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        _repository.VerifyNoOtherCalls();
        _jwtProvider.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Handle_WhenVerifiedIdentityAlreadyBelongsToProvider_ShouldIssueToken()
    {
        var identity = VerifiedGoogleIdentity();
        var user = User.CreateWithGoogle(
            identity.Email,
            "verifieduser",
            identity.FullName,
            identity.AvatarUrl,
            identity.ProviderId);
        var command = new ExternalLoginCommand(AuthProvider.Google, "valid-token");

        SetupVerifiedIdentity(command, identity);
        _repository
            .Setup(repository => repository.GetByGoogleIdAsync(
                identity.ProviderId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _jwtProvider.Setup(provider => provider.Generate(user)).Returns("jwt-token");

        var result = await CreateHandler().Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be("jwt-token");
        _repository.Verify(repository => repository.AddAsync(
            It.IsAny<User>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenEmailUsesDifferentAuthenticationMethod_ShouldNotAutoLinkAccounts()
    {
        var identity = VerifiedGoogleIdentity();
        var localUser = User.CreateWithPassword(
            identity.Email,
            "localuser",
            identity.FullName,
            "password-hash");
        var command = new ExternalLoginCommand(AuthProvider.Google, "valid-token");

        SetupVerifiedIdentity(command, identity);
        _repository
            .Setup(repository => repository.GetByGoogleIdAsync(
                identity.ProviderId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);
        _repository
            .Setup(repository => repository.GetByEmailAsync(
                identity.Email,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(localUser);

        var result = await CreateHandler().Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("different authentication method");
        _jwtProvider.VerifyNoOtherCalls();
        _repository.Verify(repository => repository.AddAsync(
            It.IsAny<User>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenVerifiedIdentityIsNew_ShouldCreateProviderBoundUser()
    {
        var identity = VerifiedGoogleIdentity();
        var command = new ExternalLoginCommand(AuthProvider.Google, "valid-token");
        User? capturedUser = null;

        SetupVerifiedIdentity(command, identity);
        _repository
            .Setup(repository => repository.GetByGoogleIdAsync(
                identity.ProviderId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);
        _repository
            .Setup(repository => repository.GetByEmailAsync(
                identity.Email,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);
        _repository
            .Setup(repository => repository.GetByUsernameAsync(
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);
        _repository
            .Setup(repository => repository.AddAsync(
                It.IsAny<User>(),
                It.IsAny<CancellationToken>()))
            .Callback<User, CancellationToken>((user, _) => capturedUser = user)
            .Returns(Task.CompletedTask);
        _jwtProvider
            .Setup(provider => provider.Generate(It.IsAny<User>()))
            .Returns("new-user-token");

        var result = await CreateHandler().Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        capturedUser.Should().NotBeNull();
        capturedUser!.Email.Should().Be(identity.Email);
        capturedUser.GoogleId.Should().Be(identity.ProviderId);
        capturedUser.Provider.Should().Be(AuthProvider.Google);
    }

    [Fact]
    public async Task Handle_WhenProviderIdentityIsConcurrentlyRegistered_ShouldReturnFailure()
    {
        var identity = VerifiedGoogleIdentity();
        var command = new ExternalLoginCommand(AuthProvider.Google, "valid-token");

        SetupVerifiedIdentity(command, identity);
        _repository
            .Setup(repository => repository.GetByGoogleIdAsync(identity.ProviderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);
        _repository
            .Setup(repository => repository.GetByEmailAsync(identity.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);
        _repository
            .Setup(repository => repository.GetByUsernameAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);
        _repository
            .Setup(repository => repository.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new DuplicateIdentityException("GoogleId", new InvalidOperationException()));

        var result = await CreateHandler().Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        _jwtProvider.Verify(provider => provider.Generate(It.IsAny<User>()), Times.Never);
    }

    private ExternalLoginCommandHandler CreateHandler() =>
        new(_repository.Object, _jwtProvider.Object, _identityVerifier.Object);

    private void SetupVerifiedIdentity(
        ExternalLoginCommand command,
        VerifiedExternalIdentity identity)
    {
        _identityVerifier
            .Setup(verifier => verifier.VerifyAsync(
                command.Provider,
                command.ProviderToken,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<VerifiedExternalIdentity>.Success(identity));
    }

    private static VerifiedExternalIdentity VerifiedGoogleIdentity() =>
        new(
            "google-subject-123",
            "verified@example.com",
            "Verified User",
            "https://example.com/avatar.png");
}
