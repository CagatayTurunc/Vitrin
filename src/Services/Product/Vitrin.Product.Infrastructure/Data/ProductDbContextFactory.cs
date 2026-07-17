using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Vitrin.Product.Infrastructure.Data;

/// <summary>
/// Design-time factory used by EF Core CLI tools (dotnet ef migrations).
/// </summary>
public class ProductDbContextFactory : IDesignTimeDbContextFactory<ProductDbContext>
{
    public ProductDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<ProductDbContext>()
            .UseNpgsql("Host=localhost;Database=vitrin_product;Username=postgres;Password=postgres")
            .Options;

        return new ProductDbContext(options);
    }
}
