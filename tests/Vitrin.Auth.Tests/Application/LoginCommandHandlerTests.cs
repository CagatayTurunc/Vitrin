using FluentAssertions;
using Moq;
using Vitrin.Auth.Application.Commands;
using Vitrin.Auth.Application.Interfaces;
using Vitrin.Auth.Domain.Entities;
using Xunit;

namespace Vitrin.Auth.Tests.Application;

public class LoginCommandHandlerTests
{
    private readonly Mock<IUserRepository> _userRepositoryMock;
    private readonly Mock<IJwtProvider> _jwtProviderMock;
    private readonly LoginCommandHandler _handler;

    public LoginCommandHandlerTests()
    {
        _userRepositoryMock = new Mock<IUserRepository>();
        _jwtProviderMock = new Mock<IJwtProvider>();
        _handler = new LoginCommandHandler(_userRepositoryMock.Object, _jwtProviderMock.Object);
    }

    [Fact]
    public async Task Handle_WithCorrectCredentials_Should_Return_Token()
    {
        // Arrange
        var password = "Password123!";
        var passwordHash = BCrypt.Net.BCrypt.HashPassword(password);
        var user = User.CreateWithPassword("user@example.com", "user", "User Name", passwordHash);

        _userRepositoryMock
            .Setup(r => r.GetByEmailAsync("user@example.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _jwtProviderMock
            .Setup(j => j.Generate(user))
            .Returns("valid-jwt-token");

        var command = new LoginCommand("user@example.com", password);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be("valid-jwt-token");
    }

    [Fact]
    public async Task Handle_WithNonExistentEmail_Should_Return_Failure()
    {
        // Arrange
        _userRepositoryMock
            .Setup(r => r.GetByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        var command = new LoginCommand("notfound@example.com", "anypassword");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be("E-posta veya şifre hatalı.");
    }

    [Fact]
    public async Task Handle_WithWrongPassword_Should_Return_Failure()
    {
        // Arrange
        var passwordHash = BCrypt.Net.BCrypt.HashPassword("correct-password");
        var user = User.CreateWithPassword("user@example.com", "user", "User", passwordHash);

        _userRepositoryMock
            .Setup(r => r.GetByEmailAsync("user@example.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var command = new LoginCommand("user@example.com", "wrong-password");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be("E-posta veya şifre hatalı.");
    }

    [Fact]
    public async Task Handle_WithGoogleUser_Trying_Local_Login_Should_Return_Failure()
    {
        // Arrange
        var googleUser = User.CreateWithGoogle("google@example.com", "googleuser", "Google User", "", "google-id-123");

        _userRepositoryMock
            .Setup(r => r.GetByEmailAsync("google@example.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(googleUser);

        var command = new LoginCommand("google@example.com", "anypassword");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be("E-posta veya şifre hatalı.");
    }
}
