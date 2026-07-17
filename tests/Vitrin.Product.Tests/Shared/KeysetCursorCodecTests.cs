using FluentAssertions;
using Vitrin.Shared.Kernel.Pagination;
using Vitrin.Shared.Kernel.Text;
using Xunit;

namespace Vitrin.Product.Tests.Shared;

public class KeysetCursorCodecTests
{
    [Fact]
    public void Encode_ThenDecode_ShouldRoundTripStableKeyset()
    {
        var timestamp = new DateTime(2026, 7, 14, 12, 34, 56, DateTimeKind.Utc).AddTicks(789);
        var id = Guid.NewGuid();

        var encoded = KeysetCursorCodec.Encode(timestamp, id);
        var decoded = KeysetCursorCodec.TryDecode(encoded, out var cursor);

        decoded.Should().BeTrue();
        cursor.TimestampUtc.Should().Be(timestamp);
        cursor.TimestampUtc.Kind.Should().Be(DateTimeKind.Utc);
        cursor.Id.Should().Be(id);
        encoded.Should().NotContain("+").And.NotContain("/").And.NotEndWith("=");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("not-a-cursor")]
    [InlineData("djI6NjM4ODgwMDAwMDAwMDAwMDAwOjAwMDAwMDAwMDAwMDAwMDAwMDAwMDAwMDAwMDAwMDAw")]
    public void TryDecode_WithMalformedOrUnsupportedCursor_ShouldFail(string? value)
    {
        KeysetCursorCodec.TryDecode(value, out _).Should().BeFalse();
    }

    [Fact]
    public void SortedCursor_ShouldRoundTripSortMetricAndStableAnchor()
    {
        var timestamp = new DateTime(2026, 7, 17, 9, 30, 0, DateTimeKind.Utc);
        var anchor = timestamp.AddMinutes(-5);
        var id = Guid.NewGuid();

        var encoded = SortedKeysetCursorCodec.Encode("trending", 42.125, timestamp, id, anchor, "filter-a1");

        SortedKeysetCursorCodec.TryDecode(encoded, out var cursor).Should().BeTrue();
        cursor.Sort.Should().Be("trending");
        cursor.Value.Should().Be(42.125);
        cursor.TimestampUtc.Should().Be(timestamp);
        cursor.AnchorUtc.Should().Be(anchor);
        cursor.Id.Should().Be(id);
        cursor.Scope.Should().Be("filter-a1");
    }

    [Theory]
    [InlineData("")]
    [InlineData("djI6dHJlbmRpbmc6TmFOOjA6MDAwMDAwMDAwMDAwMDAwMDAwMDAwMDAwMDAwMDAwMDA6MA")]
    [InlineData("not-a-sorted-cursor")]
    public void SortedCursor_WithInvalidPayload_ShouldFail(string value)
    {
        SortedKeysetCursorCodec.TryDecode(value, out _).Should().BeFalse();
    }

    [Theory]
    [InlineData("Çığ Şöleni Ürünü", "cig-soleni-urunu")]
    [InlineData("  AI / SaaS & API  ", "ai-saas-api")]
    [InlineData("***", "")]
    public void SlugGenerator_ShouldCreateTurkishSafeAsciiSlug(string value, string expected)
    {
        SlugGenerator.Generate(value).Should().Be(expected);
    }
}
