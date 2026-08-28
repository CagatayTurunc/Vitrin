namespace Vitrin.Auth.Domain.Entities;

/// <summary>
/// Tracks all payment transactions for audit and compliance (KVKK/GDPR).
/// Retained for 7 years per Turkish tax law.
/// </summary>
public class PaymentHistory
{
    public Guid Id { get; private set; }
    public Guid SubscriptionId { get; private set; }
    public Guid UserId { get; private set; } // Denormalized for quick lookup
    
    // Financial details
    public decimal Amount { get; private set; }
    public string Currency { get; private set; } = "TRY";
    
    // Payment status
    public PaymentStatus Status { get; private set; }
    public string? IyzicoPaymentId { get; private set; }
    public string? IyzicoConversationId { get; private set; }
    
    // Timestamps
    public DateTime BillingDate { get; private set; }
    public DateTime? PaidAt { get; private set; }
    public DateTime CreatedAt { get; private set; }
    
    // Failure handling
    public string? FailureReason { get; private set; }
    public string? FailureCode { get; private set; }
    public int RetryCount { get; private set; }
    public DateTime? NextRetryAt { get; private set; }
    
    // Refund tracking
    public DateTime? RefundedAt { get; private set; }
    public string? RefundReason { get; private set; }
    public decimal RefundAmount { get; private set; }

    // EF Core constructor
    protected PaymentHistory() { }

    private PaymentHistory(
        Guid id,
        Guid subscriptionId,
        Guid userId,
        decimal amount,
        string currency,
        DateTime billingDate)
    {
        Id = id;
        SubscriptionId = subscriptionId;
        UserId = userId;
        Amount = amount;
        Currency = currency;
        BillingDate = billingDate;
        Status = PaymentStatus.Pending;
        RetryCount = 0;
        CreatedAt = DateTime.UtcNow;
    }

    public static PaymentHistory Create(
        Guid subscriptionId,
        Guid userId,
        decimal amount,
        string currency,
        DateTime billingDate)
    {
        return new PaymentHistory(
            Guid.NewGuid(),
            subscriptionId,
            userId,
            amount,
            currency,
            billingDate);
    }

    public void MarkAsSucceeded(string iyzicoPaymentId, string iyzicoConversationId)
    {
        Status = PaymentStatus.Succeeded;
        IyzicoPaymentId = iyzicoPaymentId;
        IyzicoConversationId = iyzicoConversationId;
        PaidAt = DateTime.UtcNow;
    }

    public void MarkAsFailed(string failureReason, string? failureCode = null)
    {
        Status = PaymentStatus.Failed;
        FailureReason = failureReason.Trim();
        FailureCode = failureCode;
    }

    public void IncrementRetry(DateTime nextRetryAt)
    {
        RetryCount++;
        NextRetryAt = nextRetryAt;
    }

    public void MarkAsRefunded(decimal refundAmount, string reason)
    {
        if (Status != PaymentStatus.Succeeded)
            throw new InvalidOperationException("Only succeeded payments can be refunded.");

        Status = PaymentStatus.Refunded;
        RefundAmount = refundAmount;
        RefundReason = reason.Trim();
        RefundedAt = DateTime.UtcNow;
    }
}

public enum PaymentStatus
{
    Pending = 0,
    Succeeded = 1,
    Failed = 2,
    Refunded = 3
}
