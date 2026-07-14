using Vitrin.Analytics.Domain.Entities;
using Vitrin.Analytics.Domain.Repositories;
using Vitrin.Shared.Kernel.Results;
using MediatR;

namespace Vitrin.Analytics.Application.Commands;

public record TrackEventCommand(string EventType, string EventData, Guid? ProductId = null, Guid? UserId = null) : IRequest<Result<Guid>>;

public class TrackEventCommandHandler : IRequestHandler<TrackEventCommand, Result<Guid>>
{
    private readonly IAnalyticsRepository _repository;

    public TrackEventCommandHandler(IAnalyticsRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result<Guid>> Handle(TrackEventCommand request, CancellationToken cancellationToken)
    {
        var eventResult = AnalyticsEvent.Create(
            request.EventType,
            request.EventData,
            request.ProductId,
            request.UserId);

        if (!eventResult.IsSuccess)
            return Result<Guid>.Failure(eventResult.Error);

        await _repository.AddAsync(eventResult.Value, cancellationToken);

        return Result<Guid>.Success(eventResult.Value.Id);
    }
}
