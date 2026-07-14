using FluentAssertions;
using Vitrin.Auth.Domain.ValueObjects;
using Xunit;

namespace Vitrin.Auth.Tests.Domain;

public class EmailTests
{
    [Theory]
    [InlineData("user@example.com")]
    [InlineData("test.user@domain.org")]
    [InlineData("hello@sub.domain.io")]
    public void Create_WithValidEmail_Should_Succeed(string email)
    {
        // Act
        var result = Email.Create(email);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Value.Should().Be(email.ToLowerInvariant());
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Create_WithEmptyOrNull_Should_Fail(string? email)
    {
        // Act
        var result = Email.Create(email!);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be("Email cannot be empty");
    }

    [Theory]
    [InlineData("notanemail")]
    [InlineData("missing@")]
    [InlineData("@nodomain.com")]
    [InlineData("no spaces@example.com")]
    public void Create_WithInvalidFormat_Should_Fail(string email)
    {
        // Act
        var result = Email.Create(email);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be("Invalid email format");
    }
}
