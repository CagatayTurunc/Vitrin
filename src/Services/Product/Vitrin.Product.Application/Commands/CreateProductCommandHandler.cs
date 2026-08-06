using MediatR;
using Vitrin.Product.Domain.Entities;
using Vitrin.Shared.Kernel.Results;
using Vitrin.Shared.Kernel.Text;

namespace Vitrin.Product.Application.Commands;

public interface IProductRepository
{
    Task AddAsync(ProductItem product, CancellationToken cancellationToken);
    Task<ProductItem?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<bool> IsSlugUniqueAsync(string slug, CancellationToken cancellationToken);
    Task<Topic?> GetTopicBySlugAsync(string slug, CancellationToken cancellationToken);
    Task<ProductCategory?> GetCategoryBySlugAsync(string slug, CancellationToken cancellationToken);
    Task UpdateAsync(ProductItem product, CancellationToken cancellationToken);
    Task UpdateWithRevisionAsync(
        ProductItem product,
        Guid changedByUserId,
        string changedByUsername,
        string changeType,
        string? summary,
        CancellationToken cancellationToken);
    Task AddRevisionAsync(
        ProductItem product,
        Guid changedByUserId,
        string changedByUsername,
        string changeType,
        string? summary,
        CancellationToken cancellationToken);
}

public sealed class DuplicateSlugException(string resource, Exception innerException)
    : Exception($"A {resource} with the same slug already exists.", innerException)
{
    public string Resource { get; } = resource;
}

public class CreateProductCommandHandler : IRequestHandler<CreateProductCommand, Result<Guid>>
{
    private readonly IProductRepository _repository;

    public CreateProductCommandHandler(IProductRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result<Guid>> Handle(CreateProductCommand request, CancellationToken cancellationToken)
    {
        var isSlugUnique = await _repository.IsSlugUniqueAsync(request.Slug, cancellationToken);
        if (!isSlugUnique)
        {
            return Result<Guid>.Failure("This slug is already in use.");
        }

        var launchVersionLabel = string.IsNullOrWhiteSpace(request.LaunchVersionLabel)
            ? "İlk Lansman"
            : request.LaunchVersionLabel.Trim();
        var launchTagline = string.IsNullOrWhiteSpace(request.LaunchTagline)
            ? request.Tagline.Trim()
            : request.LaunchTagline.Trim();
        if (launchVersionLabel.Length > 80)
            return Result<Guid>.Failure("Launch version label cannot exceed 80 characters.");
        if (launchTagline.Length > 200)
            return Result<Guid>.Failure("Launch tagline cannot exceed 200 characters.");

        var product = ProductItem.Create(
            request.MakerId,
            request.Name,
            request.Tagline,
            request.Description,
            request.Slug,
            request.ThumbnailUrl,
            launchVersionLabel,
            launchTagline);
            
        if (request.GalleryUrls != null && request.GalleryUrls.Any())
        {
            product.SetGalleryUrls(request.GalleryUrls);
        }

        if (!string.IsNullOrWhiteSpace(request.WebsiteUrl) &&
            Uri.TryCreate(request.WebsiteUrl.Trim(), UriKind.Absolute, out var websiteUri) &&
            websiteUri.Scheme is "http" or "https")
        {
            product.AddLink("Website", websiteUri.ToString());
        }

        if (request.ScheduledLaunchAt is { } scheduledLaunchAt)
        {
            var scheduleResult = product.SetScheduledLaunch(scheduledLaunchAt);
            if (scheduleResult.IsFailure)
                return Result<Guid>.Failure(scheduleResult.Error);
        }

        // Submit for review or keep as draft based on caller preference
        if (!request.SaveAsDraft)
        {
            product.SubmitForReview();
        }

        if (request.Topics != null)
        {
            foreach (var t in request.Topics)
            {
                var topicName = t.Trim();
                if (!string.IsNullOrEmpty(topicName))
                {
                    var slug = SlugGenerator.Generate(topicName);
                    var existingTopic = await _repository.GetTopicBySlugAsync(slug, cancellationToken);
                    if (existingTopic != null)
                    {
                        product.AddTopic(existingTopic);
                    }
                    else
                    {
                        product.AddTopic(Topic.Create(topicName, slug));
                    }
                }
            }
        }

        if (request.Categories != null)
        {
            foreach (var categorySlug in request.Categories
                         .Select(value => value.Trim().ToLowerInvariant())
                         .Where(value => !string.IsNullOrWhiteSpace(value))
                         .Distinct(StringComparer.Ordinal)
                         .Take(3))
            {
                var category = await _repository.GetCategoryBySlugAsync(categorySlug, cancellationToken);
                if (category is null)
                    return Result<Guid>.Failure($"Unknown product category: {categorySlug}");

                var categoryResult = product.AddCategory(category);
                if (categoryResult.IsFailure)
                    return Result<Guid>.Failure(categoryResult.Error);
            }
        }

        try
        {
            await _repository.AddAsync(product, cancellationToken);
            await _repository.AddRevisionAsync(
                product,
                request.MakerId,
                request.RequestingUsername,
                "created",
                request.SaveAsDraft ? "Ürün taslak olarak oluşturuldu." : "Ürün oluşturuldu ve incelemeye gönderildi.",
                cancellationToken);
        }
        catch (DuplicateSlugException exception)
        {
            return Result<Guid>.Failure($"The {exception.Resource} slug is already in use. Please retry.");
        }
        
        // We can publish ProductCreatedEvent here or via an Outbox pattern in Infrastructure layer
        
        return Result<Guid>.Success(product.Id);
    }
}
