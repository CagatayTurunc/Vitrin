using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Vitrin.Product.Domain.Entities;
using Vitrin.Product.Infrastructure.Data;
using Vitrin.Product.Infrastructure.Kafka;
using Vitrin.Shared.Contracts.Events;

namespace Vitrin.Product.Infrastructure;

public sealed class ScheduledLaunchWorker(
    IServiceScopeFactory scopeFactory,
    TimeProvider timeProvider,
    ILogger<ScheduledLaunchWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await PublishDueProductsAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Scheduled product publishing failed and will be retried.");
            }

            await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
        }
    }

    internal async Task PublishDueProductsAsync(CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ProductDbContext>();
        var eventPublisher = scope.ServiceProvider.GetRequiredService<ProductEventPublisher>();
        var now = timeProvider.GetUtcNow().UtcDateTime;

        var dueProducts = await db.Products
            .Where(product =>
                product.Status == ProductStatus.Scheduled &&
                product.ScheduledLaunchAt != null &&
                product.ScheduledLaunchAt <= now)
            .OrderBy(product => product.ScheduledLaunchAt)
            .Take(50)
            .ToListAsync(cancellationToken);

        foreach (var product in dueProducts)
        {
            var publishResult = product.PublishScheduled(now);
            if (publishResult.IsFailure) continue;

            var nextRevision = (await db.ProductRevisions
                .Where(revision => revision.ProductId == product.Id)
                .Select(revision => (int?)revision.RevisionNumber)
                .MaxAsync(cancellationToken) ?? 0) + 1;
            db.ProductRevisions.Add(ProductRevision.Create(
                product,
                nextRevision,
                Guid.Empty,
                "Sistem",
                "scheduled_published",
                "Planlanan yayın zamanı geldi ve ürün otomatik yayınlandı."));

            eventPublisher.EnqueueProductPublished(new ProductPublishedEvent
            {
                ProductId = product.Id,
                MakerId = product.MakerId,
                ProductName = product.Name,
                ProductSlug = product.Slug
            });
            eventPublisher.EnqueueScheduledProductPublishedNotification(product.MakerId, product.Name, product.Id);
        }

        if (dueProducts.Count > 0)
        {
            await db.SaveChangesAsync(cancellationToken);
            logger.LogInformation("Published {Count} scheduled products.", dueProducts.Count);
        }
    }
}
