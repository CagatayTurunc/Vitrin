using FluentAssertions;
using Vitrin.Product.Domain.Entities;
using Xunit;

namespace Vitrin.Product.Tests.Domain;

public sealed class ProductCollaborationTests
{
    [Fact]
    public void Ownership_Claim_Should_Require_A_Useful_Message()
    {
        var result = ProductClaimRequest.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "maker",
            "kısa");

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void Pending_Ownership_Claim_Should_Be_Approved_Once()
    {
        var reviewerId = Guid.NewGuid();
        var claim = ProductClaimRequest.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "maker",
            "Bu ürünün doğrulanabilir kurucusuyum.").Value!;

        claim.Approve(reviewerId, "Alan adı doğrulandı.").IsSuccess.Should().BeTrue();

        claim.Status.Should().Be(ProductClaimStatus.Approved);
        claim.ReviewedByUserId.Should().Be(reviewerId);
        claim.ReviewNote.Should().Be("Alan adı doğrulandı.");
        claim.Reject(reviewerId, null).IsFailure.Should().BeTrue();
    }

    [Fact]
    public void Revision_Should_Capture_Product_Snapshot()
    {
        var makerId = Guid.NewGuid();
        var product = ProductItem.Create(makerId, "Snapshot", "İlk tagline", "İlk açıklama", "snapshot");
        product.SetGalleryUrls(["https://example.com/one.png"]);

        var revision = ProductRevision.Create(product, 1, makerId, "maker", "created");

        revision.RevisionNumber.Should().Be(1);
        revision.Name.Should().Be("Snapshot");
        revision.GalleryUrls.Should().ContainSingle("https://example.com/one.png");
        revision.Status.Should().Be(ProductStatus.Draft);
    }
}
