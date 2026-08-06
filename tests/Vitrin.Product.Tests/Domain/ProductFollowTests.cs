using FluentAssertions;
using Vitrin.Product.Domain.Entities;
using Xunit;

namespace Vitrin.Product.Tests.Domain;

public sealed class ProductFollowTests
{
    [Fact]
    public void Create_ShouldCaptureUserProductAndTimestamp()
    {
        var userId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var createdAt = new DateTime(2026, 7, 18, 12, 30, 0, DateTimeKind.Utc);

        var follow = ProductFollow.Create(userId, productId, createdAt);

        follow.UserId.Should().Be(userId);
        follow.ProductId.Should().Be(productId);
        follow.CreatedAtUtc.Should().Be(createdAt);
    }

    [Theory]
    [InlineData(true, false)]
    [InlineData(false, true)]
    public void Create_ShouldRejectEmptyIdentifiers(bool emptyUser, bool emptyProduct)
    {
        var action = () => ProductFollow.Create(
            emptyUser ? Guid.Empty : Guid.NewGuid(),
            emptyProduct ? Guid.Empty : Guid.NewGuid());

        action.Should().Throw<ArgumentException>();
    }
}
