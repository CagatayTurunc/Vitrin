using MediatR;
using Vitrin.Analytics.Domain.Repositories;
using Vitrin.Analytics.Domain.ValueObjects;
using Vitrin.Shared.Kernel.Results;

namespace Vitrin.Analytics.Application.Queries;

public record GetProductSummaryQuery(Guid ProductId) : IRequest<Result<ProductAnalyticsSummary>>;

public class GetProductSummaryQueryHandler : IRequestHandler<GetProductSummaryQuery, Result<ProductAnalyticsSummary>>
{
    private readonly IAnalyticsRepository _repository;

    public GetProductSummaryQueryHandler(IAnalyticsRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result<ProductAnalyticsSummary>> Handle(
        GetProductSummaryQuery request,
        CancellationToken cancellationToken)
    {
        var views = await _repository.CountByEventTypeAsync(
            "ProductView", request.ProductId, cancellationToken: cancellationToken);

        var upvotes = await _repository.CountByEventTypeAsync(
            "ProductUpvote", request.ProductId, cancellationToken: cancellationToken);

        var downvotes = await _repository.CountByEventTypeAsync(
            "ProductDownvote", request.ProductId, cancellationToken: cancellationToken);

        var comments = await _repository.CountByEventTypeAsync(
            "Comment", request.ProductId, cancellationToken: cancellationToken);

        var summary = new ProductAnalyticsSummary(
            ProductId:   request.ProductId,
            Views:       views,
            Upvotes:     upvotes,
            Downvotes:   downvotes,
            Comments:    comments,
            ComputedAt:  DateTime.UtcNow);

        return Result<ProductAnalyticsSummary>.Success(summary);
    }
}
