using MediatR;
using Vitrin.Analytics.Domain.Repositories;
using Vitrin.Analytics.Domain.ValueObjects;
using Vitrin.Shared.Kernel.Results;

namespace Vitrin.Analytics.Application.Queries;

public record GetPlatformSummaryQuery : IRequest<Result<PlatformAnalyticsSummary>>;

public class GetPlatformSummaryQueryHandler : IRequestHandler<GetPlatformSummaryQuery, Result<PlatformAnalyticsSummary>>
{
    private readonly IAnalyticsRepository _repository;

    public GetPlatformSummaryQueryHandler(IAnalyticsRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result<PlatformAnalyticsSummary>> Handle(
        GetPlatformSummaryQuery request,
        CancellationToken cancellationToken)
    {
        var totalViews         = await _repository.CountByEventTypeAsync("ProductView",       cancellationToken: cancellationToken);
        var totalUpvotes       = await _repository.CountByEventTypeAsync("ProductUpvote",     cancellationToken: cancellationToken);
        var totalSearches      = await _repository.CountByEventTypeAsync("Search",            cancellationToken: cancellationToken);
        var totalComments      = await _repository.CountByEventTypeAsync("Comment",           cancellationToken: cancellationToken);
        var totalRegistrations = await _repository.CountByEventTypeAsync("UserRegistered",    cancellationToken: cancellationToken);
        var totalPublished     = await _repository.CountByEventTypeAsync("ProductPublished",  cancellationToken: cancellationToken);

        var total = totalViews + totalUpvotes + totalSearches + totalComments + totalRegistrations + totalPublished;

        var summary = new PlatformAnalyticsSummary(
            TotalEvents:           total,
            TotalProductViews:     totalViews,
            TotalUpvotes:          totalUpvotes,
            TotalSearches:         totalSearches,
            TotalComments:         totalComments,
            TotalUserRegistrations: totalRegistrations,
            ComputedAt:            DateTime.UtcNow);

        return Result<PlatformAnalyticsSummary>.Success(summary);
    }
}
