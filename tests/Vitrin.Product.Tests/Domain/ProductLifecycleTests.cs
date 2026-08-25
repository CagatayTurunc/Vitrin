using FluentAssertions;
using Vitrin.Product.Domain.Entities;
using Xunit;

namespace Vitrin.Product.Tests.Domain;

/// <summary>
/// ProductItem lifecycle testleri — Reject, Archive, RetractToDraft,
/// AddCategory limit, TransferOwnership, AddOrUpdateTeamMember.
/// Mevcut ProductItemTests.cs'te Submit/Approve/Publish kapsamlı test ediliyor.
/// </summary>
public class ProductLifecycleTests
{
    private static ProductItem CreateDraftProduct(Guid? makerId = null) =>
        ProductItem.Create(
            makerId ?? Guid.NewGuid(),
            "Test Product",
            "Test tagline",
            "Test description",
            "test-product");

    // ── Reject ────────────────────────────────────────────────────────────

    [Fact]
    public void Reject_WhenUnderReview_Should_Return_Success_And_SetRejectionReason()
    {
        var product = CreateDraftProduct();
        product.SubmitForReview();

        var result = product.Reject("Ürün kılavuz ihlali.");

        result.IsSuccess.Should().BeTrue();
        product.Status.Should().Be(ProductStatus.Rejected);
        product.RejectionReason.Should().Be("Ürün kılavuz ihlali.");
    }

    [Fact]
    public void Reject_WhenDraft_Should_Return_Failure()
    {
        var product = CreateDraftProduct();

        var result = product.Reject("Neden");

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("under review");
    }

    [Fact]
    public void Reject_WithEmptyReason_Should_Return_Failure()
    {
        var product = CreateDraftProduct();
        product.SubmitForReview();

        var result = product.Reject("   ");

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("reason");
    }

    [Fact]
    public void Reject_WithReasonExceeding500Chars_Should_Return_Failure()
    {
        var product = CreateDraftProduct();
        product.SubmitForReview();

        var result = product.Reject(new string('x', 501));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("500");
    }

    // ── Archive ───────────────────────────────────────────────────────────

    [Fact]
    public void Archive_WhenPublished_Should_Return_Success()
    {
        var product = CreateDraftProduct();
        product.SubmitForReview();
        product.Approve();

        var result = product.Archive();

        result.IsSuccess.Should().BeTrue();
        product.Status.Should().Be(ProductStatus.Archived);
        product.ArchivedAt.Should().NotBeNull();
    }

    [Fact]
    public void Archive_WhenAlreadyArchived_Should_Return_Failure()
    {
        var product = CreateDraftProduct();
        product.SubmitForReview();
        product.Approve();
        product.Archive();

        var result = product.Archive();

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("already archived");
    }

    // ── RetractToDraft ────────────────────────────────────────────────────

    [Fact]
    public void RetractToDraft_WhenUnderReview_Should_Return_Success_And_Reset_Status()
    {
        var product = CreateDraftProduct();
        product.SubmitForReview();

        var result = product.RetractToDraft();

        result.IsSuccess.Should().BeTrue();
        product.Status.Should().Be(ProductStatus.Draft);
        product.RejectionReason.Should().BeNull();
    }

    [Fact]
    public void RetractToDraft_WhenRejected_Should_Return_Success()
    {
        var product = CreateDraftProduct();
        product.SubmitForReview();
        product.Reject("Neden");

        var result = product.RetractToDraft();

        result.IsSuccess.Should().BeTrue();
        product.Status.Should().Be(ProductStatus.Draft);
    }

    [Fact]
    public void RetractToDraft_WhenPublished_Should_Return_Failure()
    {
        var product = CreateDraftProduct();
        product.SubmitForReview();
        product.Approve();

        var result = product.RetractToDraft();

        result.IsFailure.Should().BeTrue();
    }

    // ── AddCategory limit ─────────────────────────────────────────────────

    [Fact]
    public void AddCategory_WhenLessThan3Categories_Should_Return_Success()
    {
        var product  = CreateDraftProduct();
        var category = ProductCategory.Create("SaaS", "saas", "Software as a Service");

        var result = product.AddCategory(category);

        result.IsSuccess.Should().BeTrue();
        product.Categories.Should().HaveCount(1);
    }

    [Fact]
    public void AddCategory_SameCategory_Twice_Should_Return_Success_Without_Duplicate()
    {
        var product    = CreateDraftProduct();
        var categoryId = Guid.NewGuid();
        var category   = ProductCategory.Create("SaaS", "saas", "Software", id: categoryId);

        product.AddCategory(category);
        var result = product.AddCategory(category);

        result.IsSuccess.Should().BeTrue();
        product.Categories.Should().HaveCount(1);
    }

    [Fact]
    public void AddCategory_When3AlreadyExist_Should_Return_Failure()
    {
        var product = CreateDraftProduct();
        product.AddCategory(ProductCategory.Create("A", "a", "A desc"));
        product.AddCategory(ProductCategory.Create("B", "b", "B desc"));
        product.AddCategory(ProductCategory.Create("C", "c", "C desc"));

        var result = product.AddCategory(ProductCategory.Create("D", "d", "D desc"));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("three");
    }

    // ── Team Member ───────────────────────────────────────────────────────

    [Fact]
    public void AddOrUpdateTeamMember_ByOwner_Should_Return_Success()
    {
        var ownerId  = Guid.NewGuid();
        var product  = CreateDraftProduct(ownerId);
        var memberId = Guid.NewGuid();

        var result = product.AddOrUpdateTeamMember(ownerId, memberId, ProductTeamRole.Editor);

        result.IsSuccess.Should().BeTrue();
        product.TeamMembers.Should().HaveCount(1);
        product.TeamMembers[0].UserId.Should().Be(memberId);
    }

    [Fact]
    public void AddOrUpdateTeamMember_ByNonOwner_Should_Return_Failure()
    {
        var product = CreateDraftProduct();

        var result = product.AddOrUpdateTeamMember(Guid.NewGuid(), Guid.NewGuid(), ProductTeamRole.Editor);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("owner");
    }

    [Fact]
    public void AddOrUpdateTeamMember_OwnerAsTeamMember_Should_Return_Failure()
    {
        var ownerId = Guid.NewGuid();
        var product = CreateDraftProduct(ownerId);

        var result = product.AddOrUpdateTeamMember(ownerId, ownerId, ProductTeamRole.Editor);

        result.IsFailure.Should().BeTrue();
    }
}
