using FluentAssertions;
using Vitrin.Voting.Domain.Entities;
using Xunit;

namespace Vitrin.Voting.Tests.Domain;

public class VoteTests
{
    [Fact]
    public void Create_Should_Create_Vote_With_Correct_Properties()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var productId = Guid.NewGuid();

        // Act
        var vote = Vote.Create(userId, productId);

        // Assert
        vote.UserId.Should().Be(userId);
        vote.ProductId.Should().Be(productId);
        vote.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        vote.Id.Should().NotBeEmpty();
    }

    [Fact]
    public void Create_TwoVotes_Should_Have_Different_Ids()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var productId = Guid.NewGuid();

        // Act
        var vote1 = Vote.Create(userId, productId);
        var vote2 = Vote.Create(userId, productId);

        // Assert
        vote1.Id.Should().NotBe(vote2.Id);
    }

    [Fact]
    public void Create_Should_Store_UserId_And_ProductId()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var productId = Guid.NewGuid();

        // Act
        var vote = Vote.Create(userId, productId);

        // Assert
        vote.UserId.Should().Be(userId);
        vote.ProductId.Should().Be(productId);
    }
}
