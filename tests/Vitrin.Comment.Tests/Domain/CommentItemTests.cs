using FluentAssertions;
using Vitrin.Comment.Domain.Entities;
using Xunit;

namespace Vitrin.Comment.Tests.Domain;

public class CommentItemTests
{
    [Fact]
    public void Create_WithValidContent_Should_Succeed()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var content = "Harika bir ürün!";

        // Act
        var result = CommentItem.Create(productId, userId, "testuser", content);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.ProductId.Should().Be(productId);
        result.Value.UserId.Should().Be(userId);
        result.Value.Content.Should().Be(content);
        result.Value.IsDeleted.Should().BeFalse();
        result.Value.Id.Should().NotBeEmpty();
        result.Value.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithEmptyContent_Should_Fail(string content)
    {
        // Act
        var result = CommentItem.Create(Guid.NewGuid(), Guid.NewGuid(), "user", content);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("empty");
    }

    [Fact]
    public void Create_WithParentComment_Should_Set_ParentCommentId()
    {
        // Arrange
        var parentId = Guid.NewGuid();

        // Act
        var result = CommentItem.Create(Guid.NewGuid(), Guid.NewGuid(), "user", "Cevap yorumu", parentId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.ParentCommentId.Should().Be(parentId);
    }

    [Fact]
    public void Create_WithoutParentComment_Should_Have_Null_ParentCommentId()
    {
        // Act
        var result = CommentItem.Create(Guid.NewGuid(), Guid.NewGuid(), "user", "Ana yorum");

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.ParentCommentId.Should().BeNull();
    }

    [Fact]
    public void UpdateContent_Should_Change_Content()
    {
        // Arrange
        var comment = CommentItem.Create(Guid.NewGuid(), Guid.NewGuid(), "user", "Eski içerik").Value;

        // Act
        var result = comment.UpdateContent("Yeni içerik");

        // Assert
        result.IsSuccess.Should().BeTrue();
        comment.Content.Should().Be("Yeni içerik");
        comment.UpdatedAt.Should().NotBeNull();
    }

    [Fact]
    public void UpdateContent_WithEmptyContent_Should_Fail()
    {
        // Arrange
        var comment = CommentItem.Create(Guid.NewGuid(), Guid.NewGuid(), "user", "İçerik").Value;

        // Act
        var result = comment.UpdateContent("");

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("empty");
    }

    [Fact]
    public void UpdateContent_On_Deleted_Comment_Should_Fail()
    {
        // Arrange
        var comment = CommentItem.Create(Guid.NewGuid(), Guid.NewGuid(), "user", "İçerik").Value;
        comment.MarkAsDeleted();

        // Act
        var result = comment.UpdateContent("Yeni içerik");

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("deleted");
    }

    [Fact]
    public void MarkAsDeleted_Should_Set_IsDeleted_True()
    {
        // Arrange
        var comment = CommentItem.Create(Guid.NewGuid(), Guid.NewGuid(), "user", "İçerik").Value;

        // Act
        comment.MarkAsDeleted();

        // Assert
        comment.IsDeleted.Should().BeTrue();
        comment.UpdatedAt.Should().NotBeNull();
    }
}
