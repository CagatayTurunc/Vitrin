using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

namespace Vitrin.Shared.Infrastructure.Audit;

public sealed record AuditEvent(
    string Action,
    Guid? ActorUserId,
    string ResourceType,
    string? ResourceId,
    string Outcome,
    string? TraceId = null,
    string? Details = null);

public interface IAuditLogger
{
    ValueTask WriteAsync(AuditEvent auditEvent, CancellationToken cancellationToken = default);
}

public sealed class StructuredAuditLogger(
    ILogger<StructuredAuditLogger> logger,
    TimeProvider timeProvider) : IAuditLogger
{
    public ValueTask WriteAsync(
        AuditEvent auditEvent,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation(
            "AUDIT {OccurredAtUtc} Action={Action} ActorUserId={ActorUserId} " +
            "ResourceType={ResourceType} ResourceId={ResourceId} Outcome={Outcome} TraceId={TraceId} Details={Details}",
            timeProvider.GetUtcNow(),
            auditEvent.Action,
            auditEvent.ActorUserId,
            auditEvent.ResourceType,
            auditEvent.ResourceId,
            auditEvent.Outcome,
            auditEvent.TraceId,
            auditEvent.Details);

        return ValueTask.CompletedTask;
    }
}

public static class AuditLoggingExtensions
{
    public static IServiceCollection AddVitrinAuditLogging(this IServiceCollection services)
    {
        services.TryAddSingleton(TimeProvider.System);
        services.TryAddSingleton<IAuditLogger, StructuredAuditLogger>();
        return services;
    }
}
