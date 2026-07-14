using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Vitrin.Shared.Infrastructure.Migrations;

public static class DatabaseMigrationExtensions
{
    public const string MigrateOnlyArgument = "--migrate-only";

    public static async Task<bool> MigrateDatabaseAndExitAsync<TDbContext>(
        this WebApplication app,
        IReadOnlyCollection<string> args,
        Func<TDbContext, CancellationToken, Task> migrateAsync,
        CancellationToken cancellationToken = default)
        where TDbContext : DbContext
    {
        if (!args.Contains(MigrateOnlyArgument, StringComparer.OrdinalIgnoreCase))
        {
            return false;
        }

        await using var scope = app.Services.CreateAsyncScope();
        var logger = scope.ServiceProvider
            .GetRequiredService<ILoggerFactory>()
            .CreateLogger("DatabaseMigration");
        var dbContext = scope.ServiceProvider.GetRequiredService<TDbContext>();

        logger.LogInformation("Applying migrations for {DbContext}.", typeof(TDbContext).Name);
        await migrateAsync(dbContext, cancellationToken);
        logger.LogInformation("Migrations completed for {DbContext}.", typeof(TDbContext).Name);
        return true;
    }
}
