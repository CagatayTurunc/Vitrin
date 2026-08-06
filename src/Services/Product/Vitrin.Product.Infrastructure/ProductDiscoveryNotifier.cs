using Microsoft.EntityFrameworkCore;
using Vitrin.Product.Domain.Entities;
using Vitrin.Product.Infrastructure.Data;
using Vitrin.Product.Infrastructure.Kafka;

namespace Vitrin.Product.Infrastructure;

public sealed class ProductDiscoveryNotifier(
    ProductDbContext db,
    ProductEventPublisher eventPublisher)
{
    public async Task EnqueueNewProductAlertsAsync(ProductItem product, CancellationToken cancellationToken)
    {
        await db.Entry(product).Collection(item => item.Topics).LoadAsync(cancellationToken);
        await db.Entry(product).Collection(item => item.Upvotes).LoadAsync(cancellationToken);

        var topicIds = product.Topics.Select(topic => topic.Id).ToList();
        if (topicIds.Count > 0)
        {
            var followers = await db.TopicFollows
                .AsNoTracking()
                .Where(follow => topicIds.Contains(follow.TopicId) && follow.UserId != product.MakerId)
                .GroupBy(follow => follow.UserId)
                .Select(group => group.Key)
                .Take(1_000)
                .ToListAsync(cancellationToken);
            var topicNames = string.Join(", ", product.Topics.Select(topic => topic.Name).Take(3));
            foreach (var userId in followers)
            {
                eventPublisher.EnqueueDiscoveryNotification(
                    userId,
                    $"Takip ettiğin {topicNames} topiclerinde yeni ürün: {product.Name}",
                    "topic_product_published",
                    product.Id);
            }
        }

        var savedSearches = await db.SavedSearches
            .AsNoTracking()
            .Where(search => search.NotifyOnNewMatches && search.UserId != product.MakerId)
            .OrderBy(search => search.CreatedAtUtc)
            .Take(1_000)
            .ToListAsync(cancellationToken);
        foreach (var search in savedSearches.Where(search => search.Matches(product)))
        {
            eventPublisher.EnqueueDiscoveryNotification(
                search.UserId,
                $"Kaydettiğin “{search.Name}” aramasına yeni eşleşme: {product.Name}",
                "saved_search_match",
                product.Id);
        }
    }
}
