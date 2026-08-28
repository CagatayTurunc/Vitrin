namespace Vitrin.Shared.Contracts.Payment;

/// <summary>
/// Payment gateway abstraction for subscription billing.
/// Implementation: IyzicoPaymentService
/// </summary>
public interface IPaymentService
{
    /// <summary>
    /// Creates a checkout session and returns the hosted payment page URL.
    /// User will be redirected to Iyzico's 3D Secure page.
    /// </summary>
    Task<CheckoutSessionResult> CreateCheckoutSessionAsync(
        CheckoutSessionRequest request,
        CancellationToken ct = default);

    /// <summary>
    /// Retrieves payment result after user completes checkout.
    /// Called by callback endpoint when Iyzico redirects back.
    /// </summary>
    Task<PaymentResult> RetrievePaymentAsync(
        string token,
        CancellationToken ct = default);

    /// <summary>
    /// Handles recurring payment webhook from Iyzico.
    /// Called monthly for active subscriptions.
    /// </summary>
    Task<WebhookResult> HandleWebhookAsync(
        string signature,
        string payload,
        CancellationToken ct = default);

    /// <summary>
    /// Cancels recurring subscription in payment gateway.
    /// User retains access until current period ends.
    /// </summary>
    Task<bool> CancelRecurringPaymentAsync(
        string subscriptionReferenceCode,
        CancellationToken ct = default);

    /// <summary>
    /// Initiates refund for a completed payment.
    /// Used for cancellations within refund window (14 days).
    /// </summary>
    Task<RefundResult> RefundPaymentAsync(
        string paymentId,
        decimal amount,
        string reason,
        CancellationToken ct = default);
}

public record CheckoutSessionRequest(
    Guid UserId,
    string Email,
    string FullName,
    string PhoneNumber,
    SubscriptionTier Tier,
    string CallbackUrl);

public record CheckoutSessionResult(
    bool Success,
    string? CheckoutUrl,
    string? Token,
    string? ErrorMessage);

public record PaymentResult(
    bool Success,
    string PaymentId,
    string ConversationId,
    decimal PaidPrice,
    string Currency,
    PaymentStatus Status,
    string? ErrorMessage);

public record WebhookResult(
    bool Valid,
    WebhookEventType EventType,
    string SubscriptionReferenceCode,
    string? PaymentId,
    PaymentStatus? Status);

public record RefundResult(
    bool Success,
    string? RefundId,
    decimal RefundedAmount,
    string? ErrorMessage);

public enum PaymentStatus
{
    Pending = 0,
    Success = 1,
    Failure = 2,
    Refunded = 3
}

public enum WebhookEventType
{
    Unknown = 0,
    SubscriptionPaymentSuccess = 1,
    SubscriptionPaymentFailure = 2,
    SubscriptionCanceled = 3,
    SubscriptionRenewed = 4
}

public enum SubscriptionTier
{
    Free = 0,
    ProMaker = 1,
    Enterprise = 2
}
