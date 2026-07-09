using Microsoft.EntityFrameworkCore;
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
        await _context.SaveChangesAsync(cancellationToken);
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

    public async Task<ProductItem?> GetByIdWithUpvotesAsync(Guid id, CancellationToken cancellationToken)
    {
        return await _context.Products
            .Include(p => p.Upvotes)
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
    }

    public async Task UpdateAsync(ProductItem product, CancellationToken cancellationToken)
    {
        _context.Products.Update(product);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
