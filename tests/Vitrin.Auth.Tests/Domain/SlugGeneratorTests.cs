using FluentAssertions;
using Vitrin.Shared.Kernel.Text;
using Xunit;

namespace Vitrin.Auth.Tests.Domain;

public class SlugGeneratorTests
{
    [Theory]
    [InlineData("Hello World",        "hello-world")]
    [InlineData("Hello  World",       "hello-world")]   // çoklu boşluk → tek tire
    [InlineData("  Hello  ",          "hello")]         // baştaki/sondaki boşluk
    [InlineData("hello-world",        "hello-world")]   // zaten slug
    [InlineData("Product (Beta)",     "product-beta")]
    [InlineData("Test.Product!v2",    "test-productv2")]
    public void Generate_WithAsciiInput_Should_Return_Expected_Slug(string input, string expected)
    {
        SlugGenerator.Generate(input).Should().Be(expected);
    }

    [Theory]
    [InlineData("Türkçe Ürün",        "turkce-urun")]
    [InlineData("Şampiyonluk Çağı",   "sampiyonluk-cagi")]
    [InlineData("ığüşöç",             "igusos")]        // tüm Türkçe karakterler
    [InlineData("İstanbul",           "istanbul")]      // İ → i (büyük İ)
    public void Generate_WithTurkishCharacters_Should_Transliterate_Correctly(string input, string expected)
    {
        SlugGenerator.Generate(input).Should().Be(expected);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null!)]
    public void Generate_WithEmptyOrNull_Should_Return_EmptyString(string? input)
    {
        SlugGenerator.Generate(input!).Should().BeEmpty();
    }

    [Fact]
    public void Generate_WithOnlySpecialChars_Should_Return_EmptyString()
    {
        SlugGenerator.Generate("!@#$%^&*()").Should().BeEmpty();
    }

    [Fact]
    public void Generate_WithLeadingAndTrailingSeparators_Should_Trim_Dashes()
    {
        SlugGenerator.Generate("-hello-world-").Should().Be("hello-world");
    }

    [Fact]
    public void Generate_ShouldNot_Have_Consecutive_Dashes()
    {
        var result = SlugGenerator.Generate("a   b   c");
        result.Should().NotContain("--");
        result.Should().Be("a-b-c");
    }

    [Fact]
    public void Generate_WithNumbers_Should_Preserve_Digits()
    {
        SlugGenerator.Generate("Product v2.0").Should().Be("product-v20");
    }

    [Fact]
    public void Generate_WithUpperCase_Should_Return_LowerCase()
    {
        SlugGenerator.Generate("MY PRODUCT").Should().Be("my-product");
    }
}
