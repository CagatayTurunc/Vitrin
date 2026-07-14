using FluentAssertions;
using Moq;
using Vitrin.Auth.Application.Commands;
using Vitrin.Auth.Application.Interfaces;
using Vitrin.Auth.Domain.Entities;
using Xunit;

namespace Vitrin.Auth.Tests.Application;

public class RegisterCommandHandlerTests
{
    private readonly Mock<IUserRepository> _userRepositoryMock;
    private readonly Mock<IJwtProvider> _jwtProviderMock;
    private readonly RegisterCommandHandler _handler;

    public RegisterCommandHandlerTests()
    {
        _userRepositoryMock = new Mock<IUserRepository>();
        _jwtProviderMock = new Mock<IJwtProvider>();
        _handler = new RegisterCommandHandler(_userRepositoryMock.Object, _jwtProviderMock.Object);
    }

    [Fact]
    public async Task Handle_WithValidData_Should_Return_Success_With_Token()
    {
        // Arrange
        var command = new RegisterCommand("new@example.com", "newuser", "New User", "Password123!");

        _userRepositoryMock
            .Setup(r => r.GetByEmailAsync(command.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        _userRepositoryMock
            .Setup(r => r.GetByUsernameAsync(command.Username, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        _userRepositoryMock
            .Setup(r => r.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _jwtProviderMock
            .Setup(j => j.Generate(It.IsAny<User>()))
            .Returns("fake-jwt-token");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be("fake-jwt-token");

        _userRepositoryMock.Verify(r => r.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()), Times.Once);
        _jwtProviderMock.Verify(j => j.Generate(It.IsAny<User>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithExistingEmail_Should_Return_Failure()
    {
        // Arrange
        var command = new RegisterCommand("existing@example.com", "newuser", "New User", "Password123!");
        var existingUser = User.CreateWithPassword("existing@example.com", "existinguser", "Existing", "hash");

        _userRepositoryMock
            .Setup(r => r.GetByEmailAsync(command.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingUser);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("e-posta");

        _userRepositoryMock.Verify(r => r.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WithExistingUsername_Should_Return_Failure()
    {
        // Arrange
        var command = new RegisterCommand("new@example.com", "takenuser", "New User", "Password123!");
        var existingUser = User.CreateWithPassword("other@example.com", "takenuser", "Existing", "hash");

        _userRepositoryMock
            .Setup(r => r.GetByEmailAsync(command.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        _userRepositoryMock
            .Setup(r => r.GetByUsernameAsync(command.Username, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingUser);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("kullanıcı adı");

        _userRepositoryMock.Verify(r => r.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenUniqueConstraintDetectsConcurrentRegistration_ShouldReturnFailure()
    {
        var command = new RegisterCommand("race@example.com", "raceuser", "Race User", "Password123!");
        _userRepositoryMock
            .Setup(repository => repository.GetByEmailAsync(command.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);
        _userRepositoryMock
            .Setup(repository => repository.GetByUsernameAsync(command.Username, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);
        _userRepositoryMock
            .Setup(repository => repository.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new DuplicateIdentityException("Email", new InvalidOperationException()));

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        _jwtProviderMock.Verify(provider => provider.Generate(It.IsAny<User>()), Times.Never);
    }
}
