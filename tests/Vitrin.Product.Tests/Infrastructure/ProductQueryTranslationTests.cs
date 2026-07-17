using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Vitrin.Product.Domain.Entities;
using Vitrin.Product.Api.Products;
using Vitrin.Product.Infrastructure.Data;
using Xunit;
using NpgsqlTypes;

namespace Vitrin.Product.Tests.Infrastructure;

public class ProductQueryTranslationTests
{
    [Fact]
    public void CursorPredicate_ShouldTranslateToPostgreSql()
    {
        var options = new DbContextOptionsBuilder<ProductDbContext>()
            .UseNpgsql("Host=localhost;Database=translation_test;Username=test;Password=test")
            .Options;
        using var db = new ProductDbContext(options);
        var cursorTimestamp = new DateTime(2026, 7, 14, 12, 0, 0, DateTimeKind.Utc);
        var cursorId = Guid.NewGuid();

        var sql = db.Products
            .AsNoTracking()
            .Where(product => product.Status == ProductStatus.Published)
            .Where(product =>
                product.PublishedAt < cursorTimestamp ||
                (product.PublishedAt == cursorTimestamp && product.Id.CompareTo(cursorId) < 0))
            .OrderByDescending(product => product.PublishedAt)
            .ThenByDescending(product => product.Id)
            .Take(21)
            .ProjectToResponse()
            .ToQueryString();

        sql.Should().Contain("ORDER BY");
        sql.Should().Contain("LIMIT");
        sql.Should().Contain("PublishedAt");
    }

    [Fact]
    public void CollectionDetailsProjection_ShouldTranslateToPostgreSql()
    {
        var options = new DbContextOptionsBuilder<ProductDbContext>()
            .UseNpgsql("Host=localhost;Database=translation_test;Username=test;Password=test")
            .Options;
        using var db = new ProductDbContext(options);

        var sql = db.Collections
            .AsNoTracking()
            .Where(collection => collection.Slug == "engineering-picks")
            .ProjectToDetailsResponse()
            .ToQueryString();

        sql.Should().Contain("Collections");
        sql.Should().Contain("Products");
        sql.Should().Contain("ProductUpvotes");
    }

    [Fact]
    public void FullText_And_Trigram_Search_ShouldTranslateToPostgreSql()
    {
        var options = new DbContextOptionsBuilder<ProductDbContext>()
            .UseNpgsql("Host=localhost;Database=translation_test;Username=test;Password=test")
            .Options;
        using var db = new ProductDbContext(options);
        const string term = "gelistirci";

        var sql = db.Products
            .AsNoTracking()
            .Where(product =>
                EF.Property<NpgsqlTsVector>(product, "SearchVector")
                    .Matches(EF.Functions.WebSearchToTsQuery("simple", term)) ||
                EF.Functions.TrigramsSimilarity(product.Name, term) >= 0.2)
            .Select(product => new
            {
                product.Id,
                Rank = EF.Property<NpgsqlTsVector>(product, "SearchVector")
                    .Rank(EF.Functions.WebSearchToTsQuery("simple", term)),
                Similarity = EF.Functions.TrigramsSimilarity(product.Name, term)
            })
            .ToQueryString();

        sql.Should().Contain("websearch_to_tsquery");
        sql.Should().Contain("similarity");
        sql.Should().Contain("SearchVector");
    }
}
