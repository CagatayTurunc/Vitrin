using MediatR;
using Vitrin.Shared.Kernel.Results;

namespace Vitrin.Product.Application.Commands;

public record CreateProductCommand(
    Guid MakerId,
    string Name,
    string Tagline,
    string Description,
    string Slug,
    List<string> Topics,
    string? ThumbnailUrl,
    List<string>? GalleryUrls,
    bool SaveAsDraft = false,
    string RequestingUsername = "",
    string? WebsiteUrl = null,
    List<string>? Categories = null,
    string? LaunchVersionLabel = null,
    string? LaunchTagline = null,
    DateTime? ScheduledLaunchAt = null) : IRequest<Result<Guid>>;
