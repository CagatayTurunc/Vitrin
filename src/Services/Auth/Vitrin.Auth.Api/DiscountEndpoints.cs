using Microsoft.EntityFrameworkCore;
using Vitrin.Auth.Domain.Entities;
using Vitrin.Auth.Infrastructure.Data;
using Vitrin.Shared.Contracts.Payment;
using Vitrin.Shared.Infrastructure.Auth;
using DomainSubscriptionTier = Vitrin.Auth.Domain.Entities.SubscriptionTier;
using ContractSubscriptionTier = Vitrin.Shared.Contracts.Payment.SubscriptionTier;

namespace Vitrin.Auth.Api;

public static class DiscountEndpoints
{
    public static void MapDiscountEndpoints(this WebApplication app)
    {
        // ─────────────────────────────────────────────────────────────
        // USER ENDPOINTS
        // ─────────────────────────────────────────────────────────────

        // POST /api/discount/validate
        // Kupon kodunun geçerli olup olmadığını kontrol eder; indirim tutarını döner.
        // Checkout sayfasında anlık önizleme için kullanılır.
        app.MapPost("/api/discount/validate", async (
            HttpContext context,
            ValidateCouponRequest request,
            AuthDbContext db) =>
        {
            var userId = context.User.GetUserId();
            if (userId is null) return Results.Unauthorized();

            var result = await ValidateCouponInternalAsync(
                request.Code, request.Tier, userId.Value, db, context.RequestAborted);

            if (!result.IsValid)
                return Results.Ok(new ValidateCouponResponse(
                    Valid: false,
                    Code: request.Code.ToUpperInvariant(),
                    DiscountType: null,
                    DiscountValue: 0,
                    DiscountAmount: 0,
                    OriginalPrice: 0,
                    FinalPrice: 0,
                    ErrorMessage: result.ErrorMessage));

            var code = await db.DiscountCodes
                .AsNoTracking()
                .FirstAsync(d => d.Code == request.Code.ToUpperInvariant().Trim(),
                    context.RequestAborted);

            var originalPrice = GetTierPrice(request.Tier);
            var finalPrice = originalPrice - result.DiscountAmount;

            return Results.Ok(new ValidateCouponResponse(
                Valid: true,
                Code: code.Code,
                DiscountType: code.Type.ToString(),
                DiscountValue: code.Value,
                DiscountAmount: result.DiscountAmount,
                OriginalPrice: originalPrice,
                FinalPrice: finalPrice,
                ErrorMessage: null));
        }).RequireAuthorization();

        // ─────────────────────────────────────────────────────────────
        // ADMIN ENDPOINTS
        // ─────────────────────────────────────────────────────────────

        // POST /api/discount/admin/create
        app.MapPost("/api/discount/admin/create", async (
            HttpContext context,
            CreateDiscountCodeRequest request,
            AuthDbContext db) =>
        {
            var adminId = context.User.GetUserId();
            if (adminId is null) return Results.Unauthorized();

            // Kod zaten var mı?
            var exists = await db.DiscountCodes
                .AnyAsync(d => d.Code == request.Code.ToUpperInvariant().Trim(),
                    context.RequestAborted);

            if (exists)
                return Results.Conflict(new { error = "Bu kupon kodu zaten mevcut." });

            var applicableTiers = request.ApplicableTiers?
                .Select(t => (DomainSubscriptionTier)t)
                .ToList() ?? [];

            DiscountCode code;
            try
            {
                code = DiscountCode.Create(
                    code: request.Code,
                    description: request.Description,
                    type: (DiscountType)request.Type,
                    value: request.Value,
                    applicableTiers: applicableTiers,
                    maxUses: request.MaxUses,
                    maxUsesPerUser: request.MaxUsesPerUser,
                    durationMonths: request.DurationMonths,
                    startsAt: request.StartsAt,
                    expiresAt: request.ExpiresAt,
                    createdByAdminId: adminId.Value);
            }
            catch (ArgumentException ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }

            db.DiscountCodes.Add(code);
            await db.SaveChangesAsync(context.RequestAborted);

            return Results.Created($"/api/discount/admin/{code.Id}", new { code.Id, code.Code });
        }).RequireAuthorization("Admin");

        // GET /api/discount/admin/list
        app.MapGet("/api/discount/admin/list", async (
            HttpContext context,
            AuthDbContext db) =>
        {
            var codes = await db.DiscountCodes
                .AsNoTracking()
                .OrderByDescending(d => d.CreatedAt)
                .Select(d => new
                {
                    d.Id,
                    d.Code,
                    d.Description,
                    Type = d.Type.ToString(),
                    d.Value,
                    ApplicableTiers = d.ApplicableTiers.Select(t => t.ToString()).ToList(),
                    d.MaxUses,
                    d.MaxUsesPerUser,
                    d.DurationMonths,
                    d.CurrentUseCount,
                    d.IsActive,
                    d.StartsAt,
                    d.ExpiresAt,
                    d.CreatedAt
                })
                .ToListAsync(context.RequestAborted);

            return Results.Ok(codes);
        }).RequireAuthorization("Admin");

        // GET /api/discount/admin/{id}/usages
        app.MapGet("/api/discount/admin/{id:guid}/usages", async (
            Guid id,
            HttpContext context,
            AuthDbContext db) =>
        {
            var usages = await db.DiscountCodeUsages
                .AsNoTracking()
                .Where(u => u.DiscountCodeId == id)
                .OrderByDescending(u => u.UsedAt)
                .Select(u => new
                {
                    u.Id,
                    u.UserId,
                    UserEmail = db.Users
                        .Where(user => user.Id == u.UserId)
                        .Select(user => user.Email)
                        .FirstOrDefault() ?? string.Empty,
                    u.DiscountApplied,
                    u.PaymentHistoryId,
                    u.UsedAt
                })
                .ToListAsync(context.RequestAborted);

            return Results.Ok(usages);
        }).RequireAuthorization("Admin");

        // PATCH /api/discount/admin/{id}/deactivate
        app.MapPatch("/api/discount/admin/{id:guid}/deactivate", async (
            Guid id,
            HttpContext context,
            AuthDbContext db) =>
        {
            var code = await db.DiscountCodes.FindAsync([id], context.RequestAborted);
            if (code is null) return Results.NotFound();

            code.Deactivate();
            await db.SaveChangesAsync(context.RequestAborted);
            return Results.NoContent();
        }).RequireAuthorization("Admin");

        // PATCH /api/discount/admin/{id}/activate
        app.MapPatch("/api/discount/admin/{id:guid}/activate", async (
            Guid id,
            HttpContext context,
            AuthDbContext db) =>
        {
            var code = await db.DiscountCodes.FindAsync([id], context.RequestAborted);
            if (code is null) return Results.NotFound();

            code.Activate();
            await db.SaveChangesAsync(context.RequestAborted);
            return Results.NoContent();
        }).RequireAuthorization("Admin");

        // PATCH /api/discount/admin/{id}
        app.MapPatch("/api/discount/admin/{id:guid}", async (
            Guid id,
            UpdateDiscountCodeRequest request,
            HttpContext context,
            AuthDbContext db) =>
        {
            var code = await db.DiscountCodes.FindAsync([id], context.RequestAborted);
            if (code is null) return Results.NotFound();

            code.Update(request.Description, request.MaxUses, request.ExpiresAt);
            await db.SaveChangesAsync(context.RequestAborted);
            return Results.NoContent();
        }).RequireAuthorization("Admin");
    }

    /// <summary>
    /// Kupon doğrulama — hem /validate endpoint'i hem checkout callback'i kullanır.
    /// </summary>
    public static async Task<DiscountValidationResult> ValidateCouponInternalAsync(
        string? couponCode,
        ContractSubscriptionTier tier,
        Guid userId,
        AuthDbContext db,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(couponCode))
            return DiscountValidationResult.Fail("Kupon kodu boş.");

        var normalizedCode = couponCode.ToUpperInvariant().Trim();

        var code = await db.DiscountCodes
            .AsNoTracking()
            .FirstOrDefaultAsync(d => d.Code == normalizedCode, ct);

        if (code is null)
            return DiscountValidationResult.Fail("Kupon kodu bulunamadı.");

        var userUseCount = await db.DiscountCodeUsages
            .CountAsync(u => u.DiscountCodeId == code.Id && u.UserId == userId, ct);

        var domainTier = (DomainSubscriptionTier)(int)tier;
        return code.Validate(userId, domainTier, userUseCount);
    }

    private static decimal GetTierPrice(ContractSubscriptionTier tier) => tier switch
    {
        ContractSubscriptionTier.ProMaker => 299m,
        ContractSubscriptionTier.Enterprise => 999m,
        _ => 0m
    };
}

// ─────────────────────────────────────────────────────────────
// Request / Response Records
// ─────────────────────────────────────────────────────────────

public record ValidateCouponRequest(string Code, ContractSubscriptionTier Tier);

public record ValidateCouponResponse(
    bool Valid,
    string Code,
    string? DiscountType,
    decimal DiscountValue,
    decimal DiscountAmount,
    decimal OriginalPrice,
    decimal FinalPrice,
    string? ErrorMessage);

public record CreateDiscountCodeRequest(
    string Code,
    string? Description,
    int Type,              // 0=Percentage, 1=FixedAmount
    decimal Value,
    List<int>? ApplicableTiers,
    int? MaxUses,
    int MaxUsesPerUser,
    int? DurationMonths,
    DateTime? StartsAt,
    DateTime? ExpiresAt);

public record UpdateDiscountCodeRequest(
    string? Description,
    int? MaxUses,
    DateTime? ExpiresAt);
