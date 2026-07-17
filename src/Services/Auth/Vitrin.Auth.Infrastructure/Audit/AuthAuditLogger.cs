using Microsoft.Extensions.Logging;
using Vitrin.Auth.Domain.Entities;
using Vitrin.Auth.Infrastructure.Data;
using Vitrin.Shared.Infrastructure.Audit;

namespace Vitrin.Auth.Infrastructure.Audit;

public sealed class AuthAuditLogger(
    AuthDbContext dbContext,
    TimeProvider timeProvider,
    ILogger<AuthAuditLogger> logger) : IAuditLogger
{
    public async ValueTask WriteAsync(
        AuditEvent auditEvent,
        CancellationToken cancellationToken = default)
    {
        var occurredAtUtc = timeProvider.GetUtcNow().UtcDateTime;
        dbContext.ModerationAuditEntries.Add(ModerationAuditEntry.Create(
            auditEvent.Action,
            auditEvent.ActorUserId,
            auditEvent.ResourceType,
            auditEvent.ResourceId,
            auditEvent.Outcome,
            auditEvent.TraceId,
            auditEvent.Details,
            occurredAtUtc));
        await dbContext.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "AUDIT {OccurredAtUtc} Action={Action} ActorUserId={ActorUserId} ResourceType={ResourceType} ResourceId={ResourceId} Outcome={Outcome}",
            occurredAtUtc,
            auditEvent.Action,
            auditEvent.ActorUserId,
            auditEvent.ResourceType,
            auditEvent.ResourceId,
            auditEvent.Outcome);
    }
}
