namespace Vitrin.Ai.Application.Services;

public sealed record AiQuotaDecision(
    bool IsAllowed,
    int RemainingRequests,
    DateTimeOffset ResetAtUtc);

public interface IAiQuotaService
{
    Task<AiQuotaDecision> TryConsumeAsync(Guid userId, CancellationToken cancellationToken);
}
