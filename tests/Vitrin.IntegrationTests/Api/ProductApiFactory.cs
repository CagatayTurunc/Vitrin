using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Vitrin.Product.Api;
using Vitrin.Product.Infrastructure.Data;

namespace Vitrin.IntegrationTests.Api;

public sealed class ProductApiFactory(string connectionString)
    : WebApplicationFactory<ProductApiAssemblyMarker>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.UseSetting("Jwt:Secret", NotificationApiFactory.JwtSecret);
        builder.UseSetting("Jwt:Issuer", "Vitrin");
        builder.UseSetting("Jwt:Audience", "Vitrin");
        builder.UseSetting("ConnectionStrings:DefaultConnection", connectionString);
        builder.UseSetting("Kafka:BootstrapServers", "127.0.0.1:1");
        builder.ConfigureLogging(logging =>
        {
            logging.ClearProviders();
            logging.SetMinimumLevel(LogLevel.Warning);
        });

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<ProductDbContext>>();
            services.RemoveAll<ProductDbContext>();
            services.RemoveAll<IHostedService>();
            services.AddDbContext<ProductDbContext>(options => options.UseNpgsql(connectionString));
        });
    }

    public async Task ApplyMigrationsAsync()
    {
        await using var scope = Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ProductDbContext>();
        await db.Database.MigrateAsync();
    }
}
