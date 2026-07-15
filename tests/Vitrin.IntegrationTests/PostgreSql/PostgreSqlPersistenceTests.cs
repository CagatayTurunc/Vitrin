using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Vitrin.Auth.Domain.Entities;
using Vitrin.Auth.Infrastructure.Data;
using Vitrin.IntegrationTests.Infrastructure;
using Vitrin.Product.Domain.Entities;
using Vitrin.Product.Infrastructure.Data;

namespace Vitrin.IntegrationTests.PostgreSql;

[Collection(PostgreSqlCollection.Name)]
[Trait("Category", "Integration")]
public sealed class PostgreSqlPersistenceTests(PostgreSqlFixture fixture) : IAsyncLifetime
{
    private string _authConnectionString = null!;
    private string _productConnectionString = null!;

    public async Task InitializeAsync()
    {
        _authConnectionString = await fixture.CreateDatabaseAsync("auth");
        _productConnectionString = await fixture.CreateDatabaseAsync("product");

        await using var auth = CreateAuthDbContext();
        await auth.Database.MigrateAsync();

        await using var product = CreateProductDbContext();
        await product.Database.MigrateAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Auth_migrations_install_citext_and_leave_no_pending_migration()
    {
        await using var db = CreateAuthDbContext();

        var pendingMigrations = await db.Database.GetPendingMigrationsAsync();
        var citextInstalled = await ScalarAsync<bool>(
            _authConnectionString,
            "SELECT EXISTS (SELECT 1 FROM pg_extension WHERE extname = 'citext')");

        pendingMigrations.Should().BeEmpty();
        citextInstalled.Should().BeTrue();
    }

    [Fact]
    public async Task Auth_identity_lookup_and_unique_index_are_case_insensitive()
    {
        var email = $"owner-{Guid.NewGuid():N}@example.com";
        var username = $"owner{Guid.NewGuid():N}"[..30];

        await using (var db = CreateAuthDbContext())
        {
            db.Users.Add(User.CreateWithPassword(email, username, "Owner", "hash"));
            await db.SaveChangesAsync();
        }

        await using (var queryDb = CreateAuthDbContext())
        {
            var found = await queryDb.Users.SingleAsync(user => user.Email == email.ToUpperInvariant());
            found.Username.Should().Be(username);
        }

        await using var duplicateDb = CreateAuthDbContext();
        duplicateDb.Users.Add(User.CreateWithPassword(email.ToUpperInvariant(), $"other{Guid.NewGuid():N}"[..30], "Other", "hash"));
        var saveDuplicate = () => duplicateDb.SaveChangesAsync();

        await saveDuplicate.Should().ThrowAsync<DbUpdateException>();
    }

    [Fact]
    public async Task Product_migrations_install_trigram_and_expected_query_indexes()
    {
        await using var db = CreateProductDbContext();

        var pendingMigrations = await db.Database.GetPendingMigrationsAsync();
        var extensionInstalled = await ScalarAsync<bool>(
            _productConnectionString,
            "SELECT EXISTS (SELECT 1 FROM pg_extension WHERE extname = 'pg_trgm')");
        var indexNames = await QueryStringsAsync(
            _productConnectionString,
            "SELECT indexname FROM pg_indexes WHERE schemaname = 'public'");

        pendingMigrations.Should().BeEmpty();
        extensionInstalled.Should().BeTrue();
        indexNames.Should().Contain([
            "IX_Products_Status_PublishedAt_Id",
            "IX_Products_Name_Trgm",
            "IX_Collections_UserId_CreatedAt",
            "UX_ProductUpvotes_ProductId_UserId"
        ]);
    }

    [Fact]
    public async Task Product_read_model_rejects_duplicate_vote_delivery()
    {
        var product = ProductItem.Create(
            Guid.NewGuid(),
            "Integration Product",
            "A real PostgreSQL constraint test",
            "Duplicate event delivery must be idempotent.",
            $"integration-{Guid.NewGuid():N}");
        var userId = Guid.NewGuid();

        await using (var db = CreateProductDbContext())
        {
            db.Products.Add(product);
            await db.SaveChangesAsync();
            db.ProductUpvotes.Add(new ProductUpvote(product.Id, userId));
            await db.SaveChangesAsync();
        }

        await using var duplicateDb = CreateProductDbContext();
        duplicateDb.ProductUpvotes.Add(new ProductUpvote(product.Id, userId));
        var saveDuplicate = () => duplicateDb.SaveChangesAsync();

        await saveDuplicate.Should().ThrowAsync<DbUpdateException>();
    }

    private AuthDbContext CreateAuthDbContext()
    {
        var options = new DbContextOptionsBuilder<AuthDbContext>()
            .UseNpgsql(_authConnectionString)
            .Options;
        return new AuthDbContext(options);
    }

    private ProductDbContext CreateProductDbContext()
    {
        var options = new DbContextOptionsBuilder<ProductDbContext>()
            .UseNpgsql(_productConnectionString)
            .Options;
        return new ProductDbContext(options);
    }

    private static async Task<T> ScalarAsync<T>(string connectionString, string sql)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(sql, connection);
        return (T)(await command.ExecuteScalarAsync())!;
    }

    private static async Task<IReadOnlyCollection<string>> QueryStringsAsync(string connectionString, string sql)
    {
        var values = new List<string>();
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(sql, connection);
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            values.Add(reader.GetString(0));
        }

        return values;
    }
}
