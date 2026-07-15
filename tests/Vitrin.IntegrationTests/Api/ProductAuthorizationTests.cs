using System.Net;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Vitrin.IntegrationTests.Infrastructure;
using Vitrin.Product.Domain.Entities;
using Vitrin.Product.Infrastructure.Data;
using ProductCollection = Vitrin.Product.Domain.Entities.Collection;

namespace Vitrin.IntegrationTests.Api;

[Collection(PostgreSqlCollection.Name)]
[Trait("Category", "Integration")]
public sealed class ProductAuthorizationTests(PostgreSqlFixture postgreSql) : IAsyncLifetime
{
    private ProductApiFactory _factory = null!;

    public async Task InitializeAsync()
    {
        var connectionString = await postgreSql.CreateDatabaseAsync("product_api");
        _factory = new ProductApiFactory(connectionString);
        await _factory.ApplyMigrationsAsync();
    }

    public Task DisposeAsync()
    {
        _factory.Dispose();
        return Task.CompletedTask;
    }

    [Fact]
    public async Task Collection_mutation_rejects_anonymous_requests()
    {
        using var client = _factory.CreateClient();

        var response = await client.PostAsync(
            $"/api/collections/{Guid.NewGuid()}/products/{Guid.NewGuid()}",
            null);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task User_cannot_add_a_product_to_another_users_collection()
    {
        var ownerId = Guid.NewGuid();
        var attackerId = Guid.NewGuid();
        var (collection, product) = await SeedCollectionAndProductAsync(ownerId);
        using var client = _factory.CreateAuthenticatedClient(attackerId);

        var response = await client.PostAsync(
            $"/api/collections/{collection.Id}/products/{product.Id}",
            null);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ProductDbContext>();
        (await db.Collections.AsNoTracking()
                .Where(item => item.Id == collection.Id)
                .Select(item => item.Products.Count)
                .SingleAsync())
            .Should().Be(0);
    }

    [Fact]
    public async Task Collection_owner_can_add_a_product_using_token_identity()
    {
        var ownerId = Guid.NewGuid();
        var (collection, product) = await SeedCollectionAndProductAsync(ownerId);
        using var client = _factory.CreateAuthenticatedClient(ownerId);

        var response = await client.PostAsync(
            $"/api/collections/{collection.Id}/products/{product.Id}",
            null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ProductDbContext>();
        (await db.Collections.AsNoTracking()
                .Where(item => item.Id == collection.Id)
                .Select(item => item.Products.Count)
                .SingleAsync())
            .Should().Be(1);
    }

    [Fact]
    public async Task Member_role_cannot_create_a_product_or_use_admin_endpoint()
    {
        using var client = _factory.CreateAuthenticatedClient(Guid.NewGuid(), "Member");
        using var body = new StringContent(
            """{"name":"Blocked","tagline":"Blocked","description":"Blocked","slug":"blocked","topics":[],"thumbnailUrl":null,"galleryUrls":null}""",
            Encoding.UTF8,
            "application/json");

        var createResponse = await client.PostAsync("/api/products", body);
        var adminResponse = await client.GetAsync("/api/products/admin/pending");

        createResponse.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        adminResponse.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Invalid_cursor_returns_stable_problem_details_contract()
    {
        using var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/products?cursor=not-a-valid-cursor");
        using var problem = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/problem+json");
        problem.RootElement.GetProperty("code").GetString().Should().Be("pagination.invalid_cursor");
        problem.RootElement.GetProperty("traceId").GetString().Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task Product_openapi_keeps_all_approved_operations()
    {
        using var client = _factory.CreateClient();
        using var actual = JsonDocument.Parse(await client.GetStringAsync("/swagger/v1/swagger.json"));
        using var baseline = JsonDocument.Parse(
            await File.ReadAllTextAsync(Path.Combine(AppContext.BaseDirectory, "Contracts", "product.openapi-baseline.json")));

        var actualPaths = actual.RootElement.GetProperty("paths");
        foreach (var operation in baseline.RootElement.GetProperty("operations").EnumerateArray())
        {
            var path = operation.GetProperty("path").GetString()!;
            var method = operation.GetProperty("method").GetString()!;
            var operationId = operation.GetProperty("operationId").GetString()!;

            actualPaths.TryGetProperty(path, out var pathDefinition).Should().BeTrue();
            pathDefinition.TryGetProperty(method, out var methodDefinition).Should().BeTrue();
            methodDefinition.GetProperty("operationId").GetString().Should().Be(operationId);
        }
    }

    private async Task<(ProductCollection Collection, ProductItem Product)> SeedCollectionAndProductAsync(Guid ownerId)
    {
        var suffix = Guid.NewGuid().ToString("N");
        var collection = ProductCollection.Create(ownerId, "Integration collection", $"collection-{suffix}", "IDOR test");
        var product = ProductItem.Create(
            Guid.NewGuid(),
            "Integration product",
            "Authorization test",
            "A product seeded by an API authorization test.",
            $"product-{suffix}");

        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ProductDbContext>();
        db.AddRange(collection, product);
        await db.SaveChangesAsync();
        return (collection, product);
    }
}
