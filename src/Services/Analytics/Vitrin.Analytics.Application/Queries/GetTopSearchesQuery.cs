using MediatR;
using Vitrin.Analytics.Domain.Repositories;
using Vitrin.Shared.Kernel.Results;

namespace Vitrin.Analytics.Application.Queries;

public record GetTopSearchesQuery(int Limit = 10, DateTime? From = null) : IRequest<Result<IReadOnlyList<TopSearchTerm>>>;

public class GetTopSearchesQueryHandler : IRequestHandler<GetTopSearchesQuery, Result<IReadOnlyList<TopSearchTerm>>>
{
    private readonly IAnalyticsRepository _repository;

    public GetTopSearchesQueryHandler(IAnalyticsRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result<IReadOnlyList<TopSearchTerm>>> Handle(
        GetTopSearchesQuery request,
        CancellationToken cancellationToken)
    {
        var terms = await _repository.GetTopSearchTermsAsync(
            request.Limit, request.From, cancellationToken);

        return Result<IReadOnlyList<TopSearchTerm>>.Success(terms);
    }
}
