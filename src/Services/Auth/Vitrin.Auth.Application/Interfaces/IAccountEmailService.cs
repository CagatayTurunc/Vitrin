using Vitrin.Auth.Domain.Entities;

namespace Vitrin.Auth.Application.Interfaces;

public enum AccountActionPurpose
{
    ConfirmEmail,
    ResetPassword
}

public sealed record AccountActionTokenClaims(
    Guid UserId,
    AccountActionPurpose Purpose,
    DateTimeOffset ExpiresAt,
    string SecurityStamp);

public interface IAccountActionTokenService
{
    string Generate(User user, AccountActionPurpose purpose, TimeSpan lifetime);
    bool TryValidate(string token, AccountActionPurpose purpose, out AccountActionTokenClaims? claims);
    bool MatchesUser(AccountActionTokenClaims claims, User user);
}

public interface IAccountEmailService
{
    Task<bool> SendEmailConfirmationAsync(User user, string token, CancellationToken cancellationToken);
    Task<bool> SendPasswordResetAsync(User user, string token, CancellationToken cancellationToken);
    Task<bool> SendMakerApprovedAsync(User user, CancellationToken cancellationToken);
    Task<bool> SendSubscriptionUpgradedAsync(User user, string tier, DateTime periodEnd, CancellationToken cancellationToken);
    Task<bool> SendSubscriptionCanceledAsync(User user, string tier, DateTime periodEnd, CancellationToken cancellationToken);
    Task<bool> SendSubscriptionRenewedAsync(User user, string tier, DateTime newPeriodEnd, decimal paidAmount, CancellationToken cancellationToken);
    Task<bool> SendSubscriptionExpiredAsync(User user, string tier, CancellationToken cancellationToken);
    Task<bool> SendPaymentFailedAsync(User user, string tier, int retryCount, DateTime? nextRetryAt, CancellationToken cancellationToken);
    Task<bool> SendSubscriptionRenewalReminderAsync(User user, string tier, DateTime periodEnd, CancellationToken cancellationToken);
    Task<bool> SendSubscriptionReactivatedAsync(User user, string tier, DateTime periodEnd, CancellationToken cancellationToken);
}
