using Microsoft.EntityFrameworkCore;
using Npgsql;
using Vitrin.Product.Application.Commands;
using Vitrin.Product.Domain.Entities;
using Vitrin.Product.Infrastructure.Data;

namespace Vitrin.Product.Infrastructure.Repositories;

public class ProductRepository : IProductRepository
{
    private readonly ProductDbContext _context;

    public ProductRepository(ProductDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(ProductItem product, CancellationToken cancellationToken)
    {
        await _context.Products.AddAsync(product, cancellationToken);
        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (ProductDatabaseErrors.TryGetUniqueConstraint(exception, out var constraint))
        {
            var resource = constraint == ProductDatabaseConstraints.TopicSlug ? "topic" : "product";
            throw new DuplicateSlugException(resource, exception);
        }
    }

    public async Task<bool> IsSlugUniqueAsync(string slug, CancellationToken cancellationToken)
    {
        var exists = await _context.Products.AnyAsync(p => p.Slug == slug, cancellationToken);
        return !exists;
    }

    public async Task<Topic?> GetTopicBySlugAsync(string slug, CancellationToken cancellationToken)
    {
        return await _context.Topics.FirstOrDefaultAsync(t => t.Slug == slug, cancellationToken);
    }

    public async Task UpdateAsync(ProductItem product, CancellationToken cancellationToken)
    {
        // Entity is already tracked, just save changes to detect additions/removals in collections
        await _context.SaveChangesAsync(cancellationToken);
    }

}

public static class ProductDatabaseConstraints
{
    public const string ProductSlug = "UX_Products_Slug";
    public const string TopicSlug = "UX_Topics_Slug";
    public const string CollectionSlug = "UX_Collections_Slug";
}

public static class ProductDatabaseErrors
{
    public static bool TryGetUniqueConstraint(DbUpdateException exception, out string? constraint)
    {
        if (exception.InnerException is PostgresException
            {
                SqlState: PostgresErrorCodes.UniqueViolation
            } postgresException &&
            postgresException.ConstraintName is
                ProductDatabaseConstraints.ProductSlug or
                ProductDatabaseConstraints.TopicSlug or
                ProductDatabaseConstraints.CollectionSlug)
        {
            constraint = postgresException.ConstraintName;
            return true;
        }

        constraint = null;
        return false;
    }
}
