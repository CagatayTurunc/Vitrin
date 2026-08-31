using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Vitrin.Auth.Application.Interfaces;
using Vitrin.Auth.Infrastructure.Data;
using Vitrin.Shared.Contracts.Events;
using Vitrin.Shared.Contracts.Payment;
using Vitrin.Shared.Infrastructure.Api;
using Vitrin.Shared.Infrastructure.Audit;
using Vitrin.Shared.Infrastructure.Auth;
using Vitrin.Shared.Infrastructure.Kafka;
using DomainSubscription = Vitrin.Auth.Domain.Entities.Subscription;
using DomainSubscriptionTier = Vitrin.Auth.Domain.Entities.SubscriptionTier;
using DomainSubscriptionStatus = Vitrin.Auth.Domain.Entities.SubscriptionStatus;
using DomainPaymentStatus = Vitrin.Auth.Domain.Entities.PaymentStatus;
using DomainPaymentMethod = Vitrin.Auth.Domain.Entities.PaymentMethod;
using DomainPaymentHistory = Vitrin.Auth.Domain.Entities.PaymentHistory;
using DomainDiscountCodeUsage = Vitrin.Auth.Domain.Entities.DiscountCodeUsage;

namespace Vitrin.Auth.Api;

public static class SubscriptionEndpoints
{
    public static void MapSubscriptionEndpoints(this WebApplication app)
    {
        var logger = app.Services.GetRequiredService<ILoggerFactory>().CreateLogger("SubscriptionEndpoints");
        // POST /api/subscription/checkout
        // Creates a checkout session and returns İyzico payment page URL
        app.MapPost("/api/subscription/checkout", async (
            HttpContext context,
            CheckoutRequest request,
            IPaymentService paymentService,
            AuthDbContext db,
            IAuditLogger auditLogger) =>
        {
            var userId = context.User.GetUserId();
            if (userId is null) return Results.Unauthorized();

            // Validate tier
            if (request.Tier == SubscriptionTier.Free)
                return ApiProblemResults.BadRequest("Cannot checkout for Free tier.", "subscription.invalid_tier");

            // Get user info
            var user = await db.Users.FindAsync([userId.Value], context.RequestAborted);
            if (user is null) return Results.Unauthorized();

            // Check for existing subscription
            var existingSubscription = await db.Subscriptions
                .FirstOrDefaultAsync(s => s.UserId == userId.Value, context.RequestAborted);

            if (existingSubscription is not null && (SubscriptionTier)((int)existingSubscription.Tier) >= request.Tier)
                return ApiProblemResults.BadRequest(
                    $"Already subscribed to {existingSubscription.Tier} or higher.",
                    "subscription.already_subscribed");

            // Kupon kodu doğrulama (opsiyonel)
            decimal? discountAmount = null;
            Guid? discountCodeId = null;
            if (!string.IsNullOrWhiteSpace(request.CouponCode))
            {
                var validation = await DiscountEndpoints.ValidateCouponInternalAsync(
                    request.CouponCode, request.Tier, userId.Value, db, context.RequestAborted);

                if (!validation.IsValid)
                    return ApiProblemResults.BadRequest(
                        validation.ErrorMessage ?? "Geçersiz kupon kodu.",
                        "subscription.invalid_coupon");

                discountAmount = validation.DiscountAmount;
                var coupon = await db.DiscountCodes
                    .FirstOrDefaultAsync(d => d.Code == request.CouponCode.ToUpperInvariant().Trim(),
                        context.RequestAborted);
                discountCodeId = coupon?.Id;
            }

            // Create checkout session
            var checkoutRequest = new CheckoutSessionRequest(
                UserId: userId.Value,
                Email: user.Email,
                FullName: user.FullName ?? user.Username,
                PhoneNumber: "+905555555555", // Default for sandbox testing
                Tier: request.Tier,
                CallbackUrl: $"{context.Request.Scheme}://{context.Request.Host}/api/subscription/callback",
                DiscountAmount: discountAmount,
                CouponCode: request.CouponCode?.ToUpperInvariant().Trim());

            var result = await paymentService.CreateCheckoutSessionAsync(checkoutRequest, context.RequestAborted);

            if (!result.Success)
            {
                await auditLogger.WriteAsync(
                    new AuditEvent("subscription.checkout_failed", userId, "Subscription", null,
                        "Failed", context.TraceIdentifier, result.ErrorMessage),
                    context.RequestAborted);

                return ApiProblemResults.BadRequest(
                    result.ErrorMessage ?? "Failed to create checkout session.",
                    "subscription.checkout_failed");
            }

            await auditLogger.WriteAsync(
                new AuditEvent("subscription.checkout_created", userId, "Subscription", null,
                    "Succeeded", context.TraceIdentifier, request.Tier.ToString()),
                context.RequestAborted);

            return Results.Ok(new
            {
                CheckoutUrl = result.CheckoutUrl,
                Token = result.Token,
                DiscountAmount = discountAmount,
                DiscountCodeId = discountCodeId
            });
        }).RequireAuthorization();

        // GET /api/subscription/callback
        // İyzico redirects here after payment completion
        app.MapGet("/api/subscription/callback", async (
            HttpContext context,
            string? token,
            IPaymentService paymentService,
            AuthDbContext db,
            IAuditLogger auditLogger,
            IEventPublisher eventPublisher,
            IAccountEmailService emailService) =>
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                return Results.Redirect("/subscription/failed?error=missing_token");
            }

            // Retrieve payment result from İyzico
            var paymentResult = await paymentService.RetrievePaymentAsync(token, context.RequestAborted);

            if (!paymentResult.Success || paymentResult.Status != PaymentStatus.Success)
            {
                await auditLogger.WriteAsync(
                    new AuditEvent("subscription.payment_failed", null, "Payment", paymentResult.PaymentId,
                        "Failed", context.TraceIdentifier, paymentResult.ErrorMessage),
                    context.RequestAborted);

                return Results.Redirect($"/subscription/failed?error={paymentResult.ErrorMessage}");
            }

            // Parse conversation ID to get user ID (we set BasketId = UserId in CreateCheckoutSessionAsync)
            if (!Guid.TryParse(paymentResult.ConversationId, out var userId))
            {
                return Results.Redirect("/subscription/failed?error=invalid_conversation_id");
            }

            // Get or create subscription
            var subscription = await db.Subscriptions
                .FirstOrDefaultAsync(s => s.UserId == userId, context.RequestAborted);

            if (subscription is null)
            {
                // First time subscriber - create new subscription
                subscription = DomainSubscription.CreateFree(userId);
                db.Subscriptions.Add(subscription);
            }

            // Upgrade subscription
            // Note: Extract tier from payment metadata (for now, assume ProMaker from pricing)
            var tier = paymentResult.PaidPrice == 299m ? DomainSubscriptionTier.ProMaker : DomainSubscriptionTier.Enterprise;
            
            subscription.Upgrade(
                newTier: tier,
                iyzicoCustomerId: paymentResult.ConversationId,
                iyzicoSubscriptionId: paymentResult.PaymentId,
                paymentMethod: DomainPaymentMethod.CreditCard);

            // Save payment history
            var paymentHistory = DomainPaymentHistory.Create(
                subscriptionId: subscription.Id,
                userId: userId,
                amount: paymentResult.PaidPrice,
                currency: paymentResult.Currency,
                billingDate: DateTime.UtcNow);
            
            paymentHistory.MarkAsSucceeded(
                iyzicoPaymentId: paymentResult.PaymentId,
                iyzicoConversationId: paymentResult.ConversationId);

            db.PaymentHistories.Add(paymentHistory);

            // Kupon kullanım kaydı — query param'dan kupon kodu gel
            var couponCode = context.Request.Query["coupon"].ToString();
            if (!string.IsNullOrWhiteSpace(couponCode))
            {
                var discountCode = await db.DiscountCodes
                    .FirstOrDefaultAsync(d => d.Code == couponCode.ToUpperInvariant().Trim(),
                        context.RequestAborted);

                if (discountCode is not null)
                {
                    var discountApplied = discountCode.CalculateDiscount(tier);
                    var usage = DomainDiscountCodeUsage.Create(discountCode.Id, userId, discountApplied);
                    usage.LinkToPayment(paymentHistory.Id);
                    db.DiscountCodeUsages.Add(usage);
                    discountCode.IncrementUseCount();
                }
            }

            await db.SaveChangesAsync(context.RequestAborted);            // Publish SubscriptionUpgradedEvent to Kafka
            // Product service bu event'i tüketerek ürünlerin MakerTierSnapshot alanını güncelleyecek
            var oldTier = subscription.Tier == tier ? "Free" : subscription.Tier.ToString();
            await eventPublisher.PublishAsync(new SubscriptionUpgradedEvent
            {
                UserId = userId,
                OldTier = oldTier,
                NewTier = tier.ToString()
            }, context.RequestAborted);

            // Email bildirimi gönder — fire and forget (email hatası ödemeyi etkilemesin)
            var user = await db.Users.FindAsync([userId], context.RequestAborted);
            if (user is not null)
            {
                _ = emailService.SendSubscriptionUpgradedAsync(
                    user,
                    tier.ToString(),
                    subscription.CurrentPeriodEnd,
                    context.RequestAborted).ContinueWith(t =>
                {
                    if (t.IsFaulted)
                        logger.LogError(t.Exception, "Subscription upgrade email failed for user {UserId}", userId);
                });
            }

            await auditLogger.WriteAsync(
                new AuditEvent("subscription.upgraded", userId, "Subscription", subscription.Id.ToString(),
                    "Succeeded", context.TraceIdentifier, tier.ToString()),
                context.RequestAborted);

            return Results.Redirect($"/subscription/success?tier={tier}");
        });

        // GET /api/subscription/me
        // Get current user's subscription
        app.MapGet("/api/subscription/me", async (
            HttpContext context,
            AuthDbContext db) =>
        {
            var userId = context.User.GetUserId();
            if (userId is null) return Results.Unauthorized();

            var subscription = await db.Subscriptions
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.UserId == userId.Value, context.RequestAborted);

            if (subscription is null)
            {
                // No subscription yet - return Free tier info
                return Results.Ok(new
                {
                    Tier = SubscriptionTier.Free,
                    Status = "Active",
                    Features = GetFeatures(SubscriptionTier.Free)
                });
            }

            return Results.Ok(new
            {
                Tier = (SubscriptionTier)(int)subscription.Tier,
                Status = subscription.Status.ToString(), // Convert to string for API response
                subscription.CurrentPeriodStart,
                subscription.CurrentPeriodEnd,
                subscription.CancelAtPeriodEnd,
                subscription.IsGrandfathered,
                subscription.GrandfatherUntil,
                Features = GetFeatures((SubscriptionTier)(int)subscription.Tier)
            });
        }).RequireAuthorization();

        // POST /api/subscription/cancel
        // Schedule cancellation at end of period
        app.MapPost("/api/subscription/cancel", async (
            HttpContext context,
            CancellationRequest request,
            AuthDbContext db,
            IAuditLogger auditLogger,
            IEventPublisher eventPublisher,
            IAccountEmailService emailService) =>
        {
            var userId = context.User.GetUserId();
            if (userId is null) return Results.Unauthorized();

            var subscription = await db.Subscriptions
                .FirstOrDefaultAsync(s => s.UserId == userId.Value, context.RequestAborted);

            if (subscription is null || subscription.Tier == DomainSubscriptionTier.Free)
                return ApiProblemResults.BadRequest("No active subscription to cancel.", "subscription.not_found");

            var canceledTier = subscription.Tier.ToString();
            var periodEnd = subscription.CurrentPeriodEnd;
            subscription.ScheduleCancellation(request.Reason ?? "User requested cancellation");
            await db.SaveChangesAsync(context.RequestAborted);

            // Publish SubscriptionCanceledEvent — Product service Free tier'a düşürecek
            await eventPublisher.PublishAsync(new SubscriptionCanceledEvent
            {
                UserId = userId.Value,
                Tier = canceledTier,
                CanceledAt = DateTime.UtcNow
            }, context.RequestAborted);

            // Email bildirimi — fire and forget
            var user = await db.Users.FindAsync([userId.Value], context.RequestAborted);
            if (user is not null)
            {
                _ = emailService.SendSubscriptionCanceledAsync(user, canceledTier, periodEnd, context.RequestAborted)
                    .ContinueWith(t =>
                    {
                        if (t.IsFaulted)
                            logger.LogError(t.Exception, "Subscription canceled email failed for user {UserId}", userId.Value);
                    });
            }

            await auditLogger.WriteAsync(
                new AuditEvent("subscription.canceled", userId, "Subscription", subscription.Id.ToString(),
                    "Succeeded", context.TraceIdentifier, request.Reason),
                context.RequestAborted);

            return Results.Ok(new
            {
                Message = "Subscription will be canceled at the end of current period.",
                subscription.CurrentPeriodEnd
            });
        }).RequireAuthorization();

        // ====================================================================
        // ADMIN ENDPOINTS
        // ====================================================================

        // GET /api/subscription/admin/list — Tüm abonelikleri listele
        app.MapGet("/api/subscription/admin/list", async (
            HttpContext context,
            AuthDbContext db) =>
        {
            var subscriptions = await db.Subscriptions
                .AsNoTracking()
                .OrderByDescending(s => s.CreatedAt)
                .Select(s => new
                {
                    s.Id,
                    s.UserId,
                    UserEmail = db.Users
                        .Where(u => u.Id == s.UserId)
                        .Select(u => u.Email)
                        .FirstOrDefault() ?? string.Empty,
                    UserFullName = db.Users
                        .Where(u => u.Id == s.UserId)
                        .Select(u => u.FullName ?? u.Username)
                        .FirstOrDefault() ?? string.Empty,
                    Tier = s.Tier.ToString(),
                    Status = s.Status.ToString(),
                    s.CurrentPeriodStart,
                    s.CurrentPeriodEnd,
                    s.CancelAtPeriodEnd,
                    s.CreatedAt
                })
                .ToListAsync(context.RequestAborted);

            return Results.Ok(subscriptions);
        }).RequireAuthorization("Admin");

        // GET /api/subscription/admin/payments — Tüm ödeme geçmişi
        app.MapGet("/api/subscription/admin/payments", async (
            HttpContext context,
            AuthDbContext db) =>
        {
            var payments = await db.PaymentHistories
                .AsNoTracking()
                .OrderByDescending(p => p.BillingDate)
                .Select(p => new
                {
                    p.Id,
                    p.UserId,
                    UserEmail = db.Users
                        .Where(u => u.Id == p.UserId)
                        .Select(u => u.Email)
                        .FirstOrDefault() ?? string.Empty,
                    p.Amount,
                    p.Currency,
                    Status = p.Status.ToString(),
                    p.BillingDate,
                    p.IyzicoPaymentId
                })
                .Take(500)
                .ToListAsync(context.RequestAborted);

            return Results.Ok(payments);
        }).RequireAuthorization("Admin");

        // GET /api/subscription/tier/{userId} — Internal endpoint (Product service kullanır)
        app.MapGet("/api/subscription/tier/{userId:guid}", async (
            Guid userId,
            AuthDbContext db,
            HttpContext context) =>
        {
            var subscription = await db.Subscriptions
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.UserId == userId, context.RequestAborted);

            var tier = subscription?.Tier.ToString() ?? "Free";
            return Results.Ok(new { tier });
        }); // Internal — auth yok (sadece internal Docker ağından erişilebilir)

        // GET /api/subscription/invoices — Kullanıcının ödeme geçmişi listesi
        app.MapGet("/api/subscription/invoices", async (
            HttpContext context,
            AuthDbContext db) =>
        {
            var userId = context.User.GetUserId();
            if (userId is null) return Results.Unauthorized();

            var payments = await db.PaymentHistories
                .AsNoTracking()
                .Where(p => p.UserId == userId.Value && p.Status == DomainPaymentStatus.Succeeded)
                .OrderByDescending(p => p.BillingDate)
                .Select(p => new
                {
                    p.Id,
                    p.Amount,
                    p.Currency,
                    Status = p.Status.ToString(),
                    p.BillingDate,
                    p.IyzicoPaymentId
                })
                .ToListAsync(context.RequestAborted);

            return Results.Ok(payments);
        }).RequireAuthorization();

        // GET /api/subscription/invoices/{paymentId}/pdf — Fatura PDF indir
        app.MapGet("/api/subscription/invoices/{paymentId:guid}/pdf", async (
            Guid paymentId,
            HttpContext context,
            AuthDbContext db) =>
        {
            var userId = context.User.GetUserId();
            if (userId is null) return Results.Unauthorized();

            // Kullanıcı sadece kendi ödeme kaydına erişebilir
            var payment = await db.PaymentHistories
                .AsNoTracking()
                .FirstOrDefaultAsync(p =>
                    p.Id == paymentId &&
                    p.UserId == userId.Value &&
                    p.Status == DomainPaymentStatus.Succeeded,
                    context.RequestAborted);

            if (payment is null)
                return Results.NotFound();

            var user = await db.Users.FindAsync([userId.Value], context.RequestAborted);
            if (user is null) return Results.Unauthorized();

            // Abonelik tier bilgisini bul
            var subscription = await db.Subscriptions
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.UserId == userId.Value, context.RequestAborted);

            // Hangi tier için ödeme yapıldığını fiyattan çıkar
            var tierLabel = payment.Amount switch
            {
                >= 900m => "Enterprise 💎",
                >= 250m => "Pro Maker 🏆",
                _ => "Abonelik"
            };

            // Kupon kullanımını bul
            var couponUsage = await db.DiscountCodeUsages
                .AsNoTracking()
                .Where(u => u.PaymentHistoryId == paymentId)
                .Select(u => new
                {
                    u.DiscountApplied,
                    CouponCode = db.DiscountCodes
                        .Where(d => d.Id == u.DiscountCodeId)
                        .Select(d => d.Code)
                        .FirstOrDefault()
                })
                .FirstOrDefaultAsync(context.RequestAborted);

            var discountAmount = couponUsage?.DiscountApplied ?? 0m;
            var originalAmount = payment.Amount + discountAmount;

            var periodStart = payment.BillingDate;
            var periodEnd = payment.BillingDate.AddMonths(1);
            var billingPeriod = $"{periodStart:dd.MM.yyyy} – {periodEnd:dd.MM.yyyy}";

            var invoiceData = new InvoiceData(
                UserFullName: user.FullName ?? user.Username,
                UserEmail: user.Email,
                TierLabel: tierLabel,
                BillingPeriod: billingPeriod,
                OriginalAmount: originalAmount,
                DiscountAmount: discountAmount,
                PaidAmount: payment.Amount,
                CouponCode: couponUsage?.CouponCode,
                PaymentDate: payment.BillingDate,
                IyzicoPaymentId: payment.IyzicoPaymentId);

            var pdfBytes = InvoicePdfService.Generate(invoiceData);

            var fileName = $"vitrin-fatura-{payment.BillingDate:yyyy-MM}.pdf";
            return Results.File(pdfBytes, "application/pdf", fileName);
        }).RequireAuthorization();

        // GET /api/subscription/admin/stats — MRR ve özet istatistikler
        app.MapGet("/api/subscription/admin/stats", async (            HttpContext context,
            AuthDbContext db) =>
        {
            var allSubs = await db.Subscriptions
                .AsNoTracking()
                .ToListAsync(context.RequestAborted);

            var totalActive = allSubs.Count(s => s.Status == DomainSubscriptionStatus.Active);
            var totalPro = allSubs.Count(s => s.Tier == DomainSubscriptionTier.ProMaker && s.Status == DomainSubscriptionStatus.Active);
            var totalEnterprise = allSubs.Count(s => s.Tier == DomainSubscriptionTier.Enterprise && s.Status == DomainSubscriptionStatus.Active);
            var totalCanceled = allSubs.Count(s => s.CancelAtPeriodEnd || s.Status == DomainSubscriptionStatus.Canceled);

            var mrr = (totalPro * 299m) + (totalEnterprise * 999m);

            var totalEverActive = allSubs.Count(s => s.Tier != DomainSubscriptionTier.Free);
            var churnRate = totalEverActive > 0
                ? (double)totalCanceled / totalEverActive * 100
                : 0;

            return Results.Ok(new
            {
                TotalActive = totalActive,
                TotalPro = totalPro,
                TotalEnterprise = totalEnterprise,
                TotalCanceled = totalCanceled,
                Mrr = mrr,
                ChurnRate = Math.Round(churnRate, 1)
            });
        }).RequireAuthorization("Admin");
    }

    private static object GetFeatures(SubscriptionTier tier)
    {
        return tier switch
        {
            SubscriptionTier.Free => new
            {
                MaxProducts = 5,
                AiQuota = 5,
                AnalyticsRetention = "7 days",
                Badge = false,
                TeamMembers = 0
            },
            SubscriptionTier.ProMaker => new
            {
                MaxProducts = -1, // Unlimited
                AiQuota = 50,
                AnalyticsRetention = "90 days",
                Badge = true,
                TeamMembers = 0
            },
            SubscriptionTier.Enterprise => new
            {
                MaxProducts = -1,
                AiQuota = 200,
                AnalyticsRetention = "1 year",
                Badge = true,
                TeamMembers = 10
            },
            _ => throw new ArgumentOutOfRangeException(nameof(tier))
        };
    }
}

public record CheckoutRequest(SubscriptionTier Tier, string? CouponCode = null);
public record CancellationRequest(string? Reason);
