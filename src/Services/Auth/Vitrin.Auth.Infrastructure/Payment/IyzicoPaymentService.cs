using System.Security.Cryptography;
using System.Text;
using Iyzipay;
using Iyzipay.Model;
using Iyzipay.Request;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Vitrin.Shared.Contracts.Payment;

namespace Vitrin.Auth.Infrastructure.Payment;

/// <summary>
/// Iyzico payment gateway implementation.
/// Handles subscription billing, 3D Secure, and recurring payments.
/// </summary>
public sealed class IyzicoPaymentService : IPaymentService
{
    private readonly Options _options;
    private readonly ILogger<IyzicoPaymentService> _logger;
    private readonly string _webhookSecret;

    public IyzicoPaymentService(
        IConfiguration configuration,
        ILogger<IyzicoPaymentService> logger)
    {
        _logger = logger;
        _webhookSecret = configuration["Iyzico:WebhookSecret"] 
            ?? throw new InvalidOperationException("Iyzico:WebhookSecret is required");

        _options = new Options
        {
            ApiKey = configuration["Iyzico:ApiKey"] 
                ?? throw new InvalidOperationException("Iyzico:ApiKey is required"),
            SecretKey = configuration["Iyzico:SecretKey"] 
                ?? throw new InvalidOperationException("Iyzico:SecretKey is required"),
            BaseUrl = configuration["Iyzico:BaseUrl"] ?? "https://sandbox-api.iyzipay.com"
        };
    }

    public async Task<CheckoutSessionResult> CreateCheckoutSessionAsync(
        CheckoutSessionRequest request,
        CancellationToken ct = default)
    {
        try
        {
            var (price, currency) = GetPricing(request.Tier);
            var conversationId = Guid.NewGuid().ToString();

            // Kupon varsa indirimli fiyatı hesapla
            decimal finalPrice;
            if (request.DiscountAmount.HasValue && request.DiscountAmount.Value > 0)
            {
                var original = decimal.Parse(price);
                finalPrice = Math.Max(0m, original - request.DiscountAmount.Value);
                _logger.LogInformation(
                    "Kupon uygulandı: Orijinal={Original}, İndirim={Discount}, Final={Final}",
                    original, request.DiscountAmount.Value, finalPrice);
            }
            else
            {
                finalPrice = decimal.Parse(price);
            }

            var finalPriceStr = finalPrice.ToString("F2", System.Globalization.CultureInfo.InvariantCulture);

            var iyzicoRequest = new CreateCheckoutFormInitializeRequest
            {
                Locale = Locale.TR.ToString(),
                ConversationId = conversationId,
                Price = finalPriceStr,
                PaidPrice = finalPriceStr,
                Currency = currency,
                BasketId = request.UserId.ToString(),
                PaymentGroup = PaymentGroup.SUBSCRIPTION.ToString(),
                CallbackUrl = request.CallbackUrl,
                EnabledInstallments = new List<int> { 1 }, // Tek çekim

                Buyer = new Buyer
                {
                    Id = request.UserId.ToString(),
                    Name = request.FullName.Split(' ').FirstOrDefault() ?? "User",
                    Surname = request.FullName.Split(' ').LastOrDefault() ?? "Name",
                    GsmNumber = request.PhoneNumber,
                    Email = request.Email,
                    IdentityNumber = "11111111111", // Test için dummy
                    RegistrationAddress = "Türkiye",
                    City = "İstanbul",
                    Country = "Turkey"
                },

                ShippingAddress = new Address
                {
                    ContactName = request.FullName,
                    City = "İstanbul",
                    Country = "Turkey",
                    Description = "Vitrin Subscription"
                },

                BillingAddress = new Address
                {
                    ContactName = request.FullName,
                    City = "İstanbul",
                    Country = "Turkey",
                    Description = "Vitrin Subscription"
                },

                BasketItems = new List<BasketItem>
                {
                    new BasketItem
                    {
                        Id = "SUB_" + request.Tier,
                        Name = $"Vitrin {request.Tier} Subscription",
                        Category1 = "Subscription",
                        ItemType = BasketItemType.VIRTUAL.ToString(),
                        Price = finalPriceStr
                    }
                }
            };

            var response = await Task.Run(() => 
                CheckoutFormInitialize.Create(iyzicoRequest, _options), ct);

            if (response.Status == "success")
            {
                _logger.LogInformation(
                    "Checkout session created: UserId={UserId}, Tier={Tier}, Token={Token}",
                    request.UserId, request.Tier, response.Token);

                return new CheckoutSessionResult(
                    Success: true,
                    CheckoutUrl: response.PaymentPageUrl,
                    Token: response.Token,
                    ErrorMessage: null);
            }

            _logger.LogWarning(
                "Checkout session failed: {ErrorCode} - {ErrorMessage}",
                response.ErrorCode, response.ErrorMessage);

            return new CheckoutSessionResult(
                Success: false,
                CheckoutUrl: null,
                Token: null,
                ErrorMessage: response.ErrorMessage);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create checkout session for UserId={UserId}", request.UserId);
            return new CheckoutSessionResult(
                Success: false,
                CheckoutUrl: null,
                Token: null,
                ErrorMessage: "Payment gateway error. Please try again.");
        }
    }

    public async Task<PaymentResult> RetrievePaymentAsync(
        string token,
        CancellationToken ct = default)
    {
        try
        {
            var request = new RetrieveCheckoutFormRequest { Token = token };
            var response = await Task.Run(() => 
                CheckoutForm.Retrieve(request, _options), ct);

            var status = response.PaymentStatus switch
            {
                "SUCCESS" => PaymentStatus.Success,
                "FAILURE" => PaymentStatus.Failure,
                _ => PaymentStatus.Pending
            };

            _logger.LogInformation(
                "Payment retrieved: PaymentId={PaymentId}, Status={Status}",
                response.PaymentId, status);

            return new PaymentResult(
                Success: status == PaymentStatus.Success,
                PaymentId: response.PaymentId ?? string.Empty,
                ConversationId: response.ConversationId ?? string.Empty,
                PaidPrice: decimal.Parse(response.PaidPrice ?? "0"),
                Currency: response.Currency ?? "TRY",
                Status: status,
                ErrorMessage: status == PaymentStatus.Failure ? response.ErrorMessage : null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve payment for token={Token}", token);
            return new PaymentResult(
                Success: false,
                PaymentId: string.Empty,
                ConversationId: string.Empty,
                PaidPrice: 0,
                Currency: "TRY",
                Status: PaymentStatus.Failure,
                ErrorMessage: "Failed to retrieve payment status.");
        }
    }

    public async Task<WebhookResult> HandleWebhookAsync(
        string signature,
        string payload,
        CancellationToken ct = default)
    {
        // Webhook signature validation
        if (!ValidateSignature(signature, payload))
        {
            _logger.LogWarning("Invalid webhook signature");
            return new WebhookResult(
                Valid: false,
                EventType: WebhookEventType.Unknown,
                SubscriptionReferenceCode: string.Empty,
                PaymentId: null,
                Status: null);
        }

        // TODO: Parse Iyzico webhook payload
        // İyzico webhook formatına göre implement edilmeli
        await Task.CompletedTask;

        return new WebhookResult(
            Valid: true,
            EventType: WebhookEventType.SubscriptionPaymentSuccess,
            SubscriptionReferenceCode: "SUB_123",
            PaymentId: "PAY_123",
            Status: PaymentStatus.Success);
    }

    public async Task<bool> CancelRecurringPaymentAsync(
        string subscriptionReferenceCode,
        CancellationToken ct = default)
    {
        try
        {
            // TODO: Iyzico subscription cancellation API call
            // İyzico recurring payment cancel endpoint'i kullanılmalı
            _logger.LogInformation("Canceling subscription: {Code}", subscriptionReferenceCode);
            
            await Task.CompletedTask;
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to cancel subscription: {Code}", subscriptionReferenceCode);
            return false;
        }
    }

    public async Task<RefundResult> RefundPaymentAsync(
        string paymentId,
        decimal amount,
        string reason,
        CancellationToken ct = default)
    {
        try
        {
            var request = new CreateRefundRequest
            {
                PaymentTransactionId = paymentId,
                Price = amount.ToString("F2"),
                Currency = Iyzipay.Model.Currency.TRY.ToString(),
                Ip = "127.0.0.1"
            };

            var response = await Task.Run(() => 
                Refund.Create(request, _options), ct);

            if (response.Status == "success")
            {
                _logger.LogInformation("Refund successful: PaymentId={PaymentId}, Amount={Amount}", 
                    paymentId, amount);

                return new RefundResult(
                    Success: true,
                    RefundId: response.PaymentId,
                    RefundedAmount: amount,
                    ErrorMessage: null);
            }

            return new RefundResult(
                Success: false,
                RefundId: null,
                RefundedAmount: 0,
                ErrorMessage: response.ErrorMessage);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Refund failed: PaymentId={PaymentId}", paymentId);
            return new RefundResult(
                Success: false,
                RefundId: null,
                RefundedAmount: 0,
                ErrorMessage: "Refund processing failed.");
        }
    }

    /// <summary>
    /// Kayıtlı kart bilgileriyle yenileme ödemesi alır.
    /// İyzico'nun stored card / non-3DS flow'unu kullanır.
    /// Not: Türkiye düzenlemeleri gereği yenileme ödemeleri 3DS gerektirmez
    /// (recurring transaction olarak sınıflandırılır).
    /// </summary>
    public async Task<ChargeResult> ChargeStoredCardAsync(
        ChargeRequest request,
        CancellationToken ct = default)
    {
        try
        {
            var (price, currency) = GetPricing(request.Tier);
            
            // İyzico recurring payment — stored card ile direkt ödeme
            var iyzicoRequest = new CreatePaymentRequest
            {
                Locale = Locale.TR.ToString(),
                ConversationId = request.ConversationId,
                Price = price,
                PaidPrice = price,
                Currency = currency,
                Installment = 1,
                BasketId = request.UserId.ToString(),
                PaymentChannel = PaymentChannel.WEB.ToString(),
                PaymentGroup = PaymentGroup.SUBSCRIPTION.ToString(),
                
                // Stored card token — initial checkout'ta kaydedilen kart
                PaymentCard = new PaymentCard
                {
                    CardUserKey = request.IyzicoCustomerId, // stored card key
                    CardToken = request.IyzicoSubscriptionId  // token from initial payment
                },
                
                Buyer = new Buyer
                {
                    Id = request.UserId.ToString(),
                    Name = request.FullName.Split(' ').FirstOrDefault() ?? "User",
                    Surname = request.FullName.Split(' ').LastOrDefault() ?? "Name",
                    Email = request.Email,
                    IdentityNumber = "11111111111",
                    RegistrationAddress = "Türkiye",
                    City = "İstanbul",
                    Country = "Turkey",
                    GsmNumber = "+905555555555"
                },
                
                ShippingAddress = new Address
                {
                    ContactName = request.FullName,
                    City = "İstanbul",
                    Country = "Turkey",
                    Description = "Vitrin Subscription Renewal"
                },
                
                BillingAddress = new Address
                {
                    ContactName = request.FullName,
                    City = "İstanbul",
                    Country = "Turkey",
                    Description = "Vitrin Subscription Renewal"
                },
                
                BasketItems = new List<BasketItem>
                {
                    new BasketItem
                    {
                        Id = "RENEWAL_" + request.Tier,
                        Name = $"Vitrin {request.Tier} Subscription Renewal",
                        Category1 = "Subscription",
                        ItemType = BasketItemType.VIRTUAL.ToString(),
                        Price = price
                    }
                }
            };
            
            var response = await Task.Run(() =>
                Iyzipay.Model.Payment.Create(iyzicoRequest, _options), ct);
            
            if (response.Status == "success")
            {
                _logger.LogInformation(
                    "Yenileme ödemesi başarılı: UserId={UserId}, Tier={Tier}, PaymentId={PaymentId}",
                    request.UserId, request.Tier, response.PaymentId);
                
                return new ChargeResult(
                    Success: true,
                    PaymentId: response.PaymentId,
                    ConversationId: response.ConversationId,
                    PaidPrice: decimal.Parse(price),
                    Currency: currency,
                    ErrorMessage: null,
                    ErrorCode: null);
            }
            
            _logger.LogWarning(
                "Yenileme ödemesi başarısız: UserId={UserId}, ErrorCode={Code}, Error={Msg}",
                request.UserId, response.ErrorCode, response.ErrorMessage);
            
            return new ChargeResult(
                Success: false,
                PaymentId: null,
                ConversationId: response.ConversationId,
                PaidPrice: 0,
                Currency: currency,
                ErrorMessage: response.ErrorMessage,
                ErrorCode: response.ErrorCode);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Yenileme ödemesi sırasında hata: UserId={UserId}", request.UserId);
            return new ChargeResult(
                Success: false,
                PaymentId: null,
                ConversationId: request.ConversationId,
                PaidPrice: 0,
                Currency: "TRY",
                ErrorMessage: "Ödeme gateway hatası. Lütfen tekrar deneyiniz.",
                ErrorCode: "GATEWAY_ERROR");
        }
    }

    private static (string price, string currency) GetPricing(SubscriptionTier tier)    {
        return tier switch
        {
            SubscriptionTier.ProMaker => ("299.00", Iyzipay.Model.Currency.TRY.ToString()),
            SubscriptionTier.Enterprise => ("999.00", Iyzipay.Model.Currency.TRY.ToString()),
            _ => throw new ArgumentException($"Invalid tier for checkout: {tier}")
        };
    }

    private bool ValidateSignature(string signature, string payload)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(_webhookSecret));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
        var computed = Convert.ToBase64String(hash);
        return signature == computed;
    }
}
