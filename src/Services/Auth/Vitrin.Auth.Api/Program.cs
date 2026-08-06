using MediatR;
using Microsoft.EntityFrameworkCore;
using Vitrin.Auth.Application;
using Vitrin.Auth.Application.Commands;
using Vitrin.Auth.Infrastructure;
using Vitrin.Auth.Api;
using Vitrin.Shared.Infrastructure.Api;
using Vitrin.Shared.Infrastructure.Audit;
using Vitrin.Shared.Infrastructure.Auth;
using Vitrin.Shared.Infrastructure.Migrations;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHealthChecks();
builder.Services.AddVitrinApiErrors();
builder.Services.AddVitrinAuditLogging();

builder.Services.AddVitrinJwtAuthentication(builder.Configuration);

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

var app = builder.Build();

app.UseVitrinApiErrors();

if (await app.MigrateDatabaseAndExitAsync<Vitrin.Auth.Infrastructure.Data.AuthDbContext>(
    args,
    static (db, cancellationToken) => db.Database.MigrateAsync(cancellationToken))) return;

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthentication();
app.UseAuthorization();

app.MapHealthChecks("/health");

app.MapPost("/api/auth/register", async (RegisterCommand command, HttpContext context, IMediator mediator, IAuditLogger auditLogger) =>
{
    var result = await mediator.Send(command);
    await auditLogger.WriteAsync(
        new AuditEvent("auth.register", null, "Session", null, result.IsSuccess ? "Succeeded" : "Failed", context.TraceIdentifier),
        context.RequestAborted);
    return result.IsSuccess ? Results.Ok(result.Value) : ApiProblemResults.BadRequest(result.Error, "auth.registration_failed");
})
.AddEndpointFilter<ValidationEndpointFilter<RegisterCommand>>();

app.MapPost("/api/auth/login", async (LoginCommand command, HttpContext context, IMediator mediator, IAuditLogger auditLogger) =>
{
    var result = await mediator.Send(command);
    await auditLogger.WriteAsync(
        new AuditEvent("auth.login", null, "Session", null, result.IsSuccess ? "Succeeded" : "Failed", context.TraceIdentifier),
        context.RequestAborted);
    return result.IsSuccess ? Results.Ok(result.Value) : ApiProblemResults.BadRequest(result.Error, "auth.login_failed");
})
.AddEndpointFilter<ValidationEndpointFilter<LoginCommand>>();

app.MapPost("/api/auth/confirm-email", async (
    [Microsoft.AspNetCore.Mvc.FromBody] EmailTokenRequest request,
    Vitrin.Auth.Application.Interfaces.IAccountActionTokenService tokenService,
    Vitrin.Auth.Application.Interfaces.IUserRepository userRepository,
    CancellationToken cancellationToken) =>
{
    if (!tokenService.TryValidate(
            request.Token,
            Vitrin.Auth.Application.Interfaces.AccountActionPurpose.ConfirmEmail,
            out var claims) || claims is null)
        return ApiProblemResults.BadRequest("Doğrulama bağlantısı geçersiz veya süresi dolmuş.", "auth.email_token_invalid");

    var user = await userRepository.GetByIdAsync(claims.UserId, cancellationToken);
    if (user is null)
        return ApiProblemResults.BadRequest("Doğrulama bağlantısı geçersiz veya süresi dolmuş.", "auth.email_token_invalid");

    if (user.IsEmailConfirmed)
        return Results.Ok(new { Message = "E-posta adresin zaten doğrulanmış." });

    if (!tokenService.MatchesUser(claims, user))
        return ApiProblemResults.BadRequest("Doğrulama bağlantısı geçersiz veya süresi dolmuş.", "auth.email_token_invalid");

    user.ConfirmEmail(DateTime.UtcNow);
    await userRepository.UpdateAsync(user, cancellationToken);
    return Results.Ok(new { Message = "E-posta adresin doğrulandı. Artık giriş yapabilirsin." });
});

app.MapPost("/api/auth/resend-confirmation", async (
    [Microsoft.AspNetCore.Mvc.FromBody] EmailAddressRequest request,
    Vitrin.Auth.Application.Interfaces.IAccountActionTokenService tokenService,
    Vitrin.Auth.Application.Interfaces.IAccountEmailService emailService,
    Vitrin.Auth.Application.Interfaces.IUserRepository userRepository,
    CancellationToken cancellationToken) =>
{
    var user = string.IsNullOrWhiteSpace(request.Email)
        ? null
        : await userRepository.GetByEmailAsync(request.Email, cancellationToken);
    if (user is { Provider: Vitrin.Auth.Domain.Entities.AuthProvider.Local, IsEmailConfirmed: false })
    {
        var token = tokenService.Generate(
            user,
            Vitrin.Auth.Application.Interfaces.AccountActionPurpose.ConfirmEmail,
            TimeSpan.FromHours(24));
        await emailService.SendEmailConfirmationAsync(user, token, cancellationToken);
    }

    return Results.Ok(new { Message = "Hesap uygunsa doğrulama e-postası gönderildi." });
});

app.MapPost("/api/auth/forgot-password", async (
    [Microsoft.AspNetCore.Mvc.FromBody] EmailAddressRequest request,
    Vitrin.Auth.Application.Interfaces.IAccountActionTokenService tokenService,
    Vitrin.Auth.Application.Interfaces.IAccountEmailService emailService,
    Vitrin.Auth.Application.Interfaces.IUserRepository userRepository,
    CancellationToken cancellationToken) =>
{
    var user = string.IsNullOrWhiteSpace(request.Email)
        ? null
        : await userRepository.GetByEmailAsync(request.Email, cancellationToken);
    if (user is { Provider: Vitrin.Auth.Domain.Entities.AuthProvider.Local, IsAnonymized: false })
    {
        var token = tokenService.Generate(
            user,
            Vitrin.Auth.Application.Interfaces.AccountActionPurpose.ResetPassword,
            TimeSpan.FromHours(1));
        await emailService.SendPasswordResetAsync(user, token, cancellationToken);
    }

    return Results.Ok(new { Message = "Bu e-posta ile bir hesap varsa şifre yenileme bağlantısı gönderildi." });
});

app.MapPost("/api/auth/reset-password", async (
    [Microsoft.AspNetCore.Mvc.FromBody] ResetPasswordRequest request,
    Vitrin.Auth.Application.Interfaces.IAccountActionTokenService tokenService,
    Vitrin.Auth.Application.Interfaces.IUserRepository userRepository,
    CancellationToken cancellationToken) =>
{
    var passwordIsValid = !string.IsNullOrEmpty(request.Password) &&
                          request.Password.Length is >= 12 and <= 128 &&
                          request.Password.Any(char.IsUpper) &&
                          request.Password.Any(char.IsLower) &&
                          request.Password.Any(char.IsDigit) &&
                          request.Password.Any(character => !char.IsLetterOrDigit(character));
    if (!passwordIsValid)
        return ApiProblemResults.BadRequest(
            "Şifre 12-128 karakter olmalı; büyük harf, küçük harf, rakam ve özel karakter içermelidir.",
            "auth.password_weak");

    if (!tokenService.TryValidate(
            request.Token,
            Vitrin.Auth.Application.Interfaces.AccountActionPurpose.ResetPassword,
            out var claims) || claims is null)
        return ApiProblemResults.BadRequest("Şifre yenileme bağlantısı geçersiz veya süresi dolmuş.", "auth.password_token_invalid");

    var user = await userRepository.GetByIdAsync(claims.UserId, cancellationToken);
    if (user is null || user.Provider != Vitrin.Auth.Domain.Entities.AuthProvider.Local ||
        !tokenService.MatchesUser(claims, user))
        return ApiProblemResults.BadRequest("Şifre yenileme bağlantısı geçersiz veya süresi dolmuş.", "auth.password_token_invalid");

    user.ChangePassword(BCrypt.Net.BCrypt.HashPassword(request.Password));
    await userRepository.UpdateAsync(user, cancellationToken);
    return Results.Ok(new { Message = "Şifren yenilendi. Yeni şifrenle giriş yapabilirsin." });
});

app.MapPost("/api/auth/external-login", async (ExternalLoginCommand command, HttpContext context, IMediator mediator, IAuditLogger auditLogger) =>
{
    var result = await mediator.Send(command);
    await auditLogger.WriteAsync(
        new AuditEvent("auth.external_login", null, "Session", null, result.IsSuccess ? "Succeeded" : "Failed", context.TraceIdentifier),
        context.RequestAborted);
    return result.IsSuccess ? Results.Ok(result.Value) : ApiProblemResults.BadRequest(result.Error, "auth.external_login_failed");
})
.AddEndpointFilter<ValidationEndpointFilter<ExternalLoginCommand>>();

app.MapGet("/api/auth/admin/users", async (Vitrin.Auth.Infrastructure.Data.AuthDbContext db) =>
{
    var users = await db.Users
        .AsNoTracking()
        .OrderByDescending(user => user.CreatedAt)
        .Take(500)
        .Select(user => new {
            user.Id,
            user.Email,
            user.Username,
            user.FullName,
            user.Headline,
            user.Role,
            user.CreatedAt,
            user.ActiveBanId,
            user.SuspendedUntilUtc,
            user.SuspensionReason,
            IsBanned = user.ActiveBanId.HasValue && (!user.SuspendedUntilUtc.HasValue || user.SuspendedUntilUtc > DateTime.UtcNow)
        })
        .ToListAsync();
    return Results.Ok(users);
}).RequireAuthorization(VitrinAuthDefaults.AdminPolicy);

app.MapGet("/api/auth/users/resolve", async (string usernames, Vitrin.Auth.Infrastructure.Data.AuthDbContext db) =>
{
    var requested = usernames
        .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
        .Select(username => username.TrimStart('@').ToLowerInvariant())
        .Distinct(StringComparer.OrdinalIgnoreCase)
        .Take(10)
        .ToArray();
    if (requested.Length == 0) return Results.Ok(Array.Empty<object>());

    var users = await db.Users
        .AsNoTracking()
        .Where(user => requested.Contains(user.Username))
        .Select(user => new { UserId = user.Id, user.Username })
        .ToListAsync();
    return Results.Ok(users);
});

app.MapGet("/api/auth/users/by-username/{username}", async (string username, HttpContext context, Vitrin.Auth.Infrastructure.Data.AuthDbContext db) =>
{
    var normalizedUsername = username.Trim();
    var user = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.FirstOrDefaultAsync(
        db.Users.AsNoTracking().Include(u => u.Badges), u => u.Username == normalizedUsername);
        
    if (user == null) return Results.NotFound();
    
    var followerCount = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.CountAsync(db.UserFollows, uf => uf.FollowingId == user.Id);
    var followingCount = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.CountAsync(db.UserFollows, uf => uf.FollowerId == user.Id);
    
    bool isFollowing = false;
    var currentUserId = context.User.GetUserId();
    if (currentUserId.HasValue)
    {
        isFollowing = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.AnyAsync(db.UserFollows, uf => uf.FollowerId == currentUserId.Value && uf.FollowingId == user.Id);
    }

    return Results.Ok(new {
        user.Id,
        user.Username,
        user.FullName,
        user.AvatarUrl,
        user.Headline,
        user.About,
        user.WebsiteUrl,
        user.GithubUrl,
        user.LinkedInUrl,
        user.Role,
        user.CreatedAt,
        user.CurrentStreak,
        Badges = user.Badges.Select(b => new { b.Name, b.Icon, b.EarnedAt }),
        FollowerCount = followerCount,
        FollowingCount = followingCount,
        IsFollowing = isFollowing
    });
});

app.MapGet("/api/auth/users/{userId:guid}", async (Guid userId, HttpContext context, Vitrin.Auth.Infrastructure.Data.AuthDbContext db) =>
{
    var user = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.FirstOrDefaultAsync(db.Users.AsNoTracking().Include(u => u.Badges), u => u.Id == userId);
    if (user == null) return Results.NotFound();
    
    var followerCount = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.CountAsync(db.UserFollows, uf => uf.FollowingId == user.Id);
    var followingCount = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.CountAsync(db.UserFollows, uf => uf.FollowerId == user.Id);
    
    bool isFollowing = false;
    var currentUserId = context.User.GetUserId();
    if (currentUserId.HasValue)
    {
        isFollowing = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.AnyAsync(db.UserFollows, uf => uf.FollowerId == currentUserId.Value && uf.FollowingId == user.Id);
    }

    return Results.Ok(new {
        user.Id,
        user.Username,
        user.FullName,
        user.AvatarUrl,
        user.Headline,
        user.About,
        user.WebsiteUrl,
        user.GithubUrl,
        user.LinkedInUrl,
        user.Role,
        user.CreatedAt,
        user.CurrentStreak,
        Badges = user.Badges.Select(b => new { b.Name, b.Icon, b.EarnedAt }),
        FollowerCount = followerCount,
        FollowingCount = followingCount,
        IsFollowing = isFollowing
    });
});

app.MapGet("/api/auth/users/me", async (HttpContext context, Vitrin.Auth.Infrastructure.Data.AuthDbContext db) =>
{
    var userId = context.User.GetUserId();
    if (userId == null) return Results.Unauthorized();
    
    var user = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.FirstOrDefaultAsync(db.Users.AsNoTracking().Include(u => u.Badges), u => u.Id == userId.Value);
    if (user == null) return Results.NotFound();

    var followerCount = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.CountAsync(db.UserFollows, uf => uf.FollowingId == user.Id);
    var followingCount = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.CountAsync(db.UserFollows, uf => uf.FollowerId == user.Id);

    return Results.Ok(new {
        user.Id,
        user.Username,
        user.FullName,
        user.AvatarUrl,
        user.Headline,
        user.About,
        user.WebsiteUrl,
        user.GithubUrl,
        user.LinkedInUrl,
        user.Role,
        user.CreatedAt,
        user.CurrentStreak,
        Badges = user.Badges.Select(b => new { b.Name, b.Icon, b.EarnedAt }),
        FollowerCount = followerCount,
        FollowingCount = followingCount
        ,
        user.ActiveBanId,
        user.SuspendedUntilUtc,
        user.SuspensionReason,
        IsBanned = user.ActiveBanId.HasValue && (!user.SuspendedUntilUtc.HasValue || user.SuspendedUntilUtc > DateTime.UtcNow)
    });
}).RequireAuthorization();

app.MapPut("/api/auth/users/me", async (HttpContext context, [Microsoft.AspNetCore.Mvc.FromBody] UpdateProfileRequest request, Vitrin.Auth.Infrastructure.Data.AuthDbContext db, IAuditLogger auditLogger) =>
{
    var userId = context.User.GetUserId();
    if (userId == null) return Results.Unauthorized();

    var user = await db.Users.FindAsync(userId.Value);
    if (user == null) return Results.NotFound();

    // Check if new username is taken by someone else
    if (request.Username != user.Username && await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.AnyAsync(db.Users, u => u.Username == request.Username && u.Id != userId.Value))
    {
        return ApiProblemResults.BadRequest("This username is already taken.", "profile.username_taken");
    }

    user.UpdateProfile(request.FullName, request.Username, request.Headline, request.About, request.AvatarUrl, request.WebsiteUrl, request.GithubUrl, request.LinkedInUrl);
    await db.SaveChangesAsync();

    await auditLogger.WriteAsync(
        new AuditEvent("user.profile_updated", userId, "User", userId.ToString(), "Succeeded", context.TraceIdentifier),
        context.RequestAborted);

    return Results.Ok(new { Message = "Profile updated successfully." });
}).RequireAuthorization();

app.MapPost("/api/auth/admin/users/{id}/role", async (Guid id, [Microsoft.AspNetCore.Mvc.FromBody] int role, HttpContext context, Vitrin.Auth.Infrastructure.Data.AuthDbContext db, IAuditLogger auditLogger) =>
{
    if (!Enum.IsDefined(typeof(Vitrin.Auth.Domain.Entities.UserRole), role))
        return ApiProblemResults.BadRequest("The requested role is invalid.", "admin.invalid_role");

    var user = await db.Users.FindAsync(id);
    if (user == null) return ApiProblemResults.NotFound("User not found.", "user.not_found");
    
    user.UpdateRole((Vitrin.Auth.Domain.Entities.UserRole)role);
    await db.SaveChangesAsync();

    await auditLogger.WriteAsync(
        new AuditEvent("admin.user_role_updated", context.User.GetUserId(), "User", id.ToString(), "Succeeded", context.TraceIdentifier),
        context.RequestAborted);
    
    return Results.Ok(new { Message = "User role updated successfully" });
}).RequireAuthorization(VitrinAuthDefaults.AdminPolicy);

// MAKER APPLICATIONS
app.MapPost("/api/auth/maker-applications", async (HttpContext context, [Microsoft.AspNetCore.Mvc.FromBody] MakerApplicationRequest request, Vitrin.Auth.Infrastructure.Data.AuthDbContext db) =>
{
    var userId = context.User.GetUserId();
    if (userId is null) return Results.Unauthorized();

    var application = Vitrin.Auth.Domain.Entities.MakerApplication.Create(userId.Value, request.PortfolioUrl, request.Reason);
    db.MakerApplications.Add(application);
    await db.SaveChangesAsync();
    return Results.Ok(new { Message = "Application submitted successfully." });
}).RequireAuthorization();

app.MapGet("/api/auth/admin/maker-applications", async (Vitrin.Auth.Infrastructure.Data.AuthDbContext db) =>
{
    var result = await (
        from application in db.MakerApplications.AsNoTracking()
        join user in db.Users.AsNoTracking() on application.UserId equals user.Id
        where application.Status == Vitrin.Auth.Domain.Entities.ApplicationStatus.Pending
        orderby application.CreatedAt
        select new {
            application.Id,
            application.PortfolioUrl,
            application.Reason,
            application.CreatedAt,
            User = user.Email,
            user.FullName
        })
        .Take(100)
        .ToListAsync();
    
    return Results.Ok(result);
}).RequireAuthorization(VitrinAuthDefaults.AdminPolicy);

app.MapPost("/api/auth/admin/maker-applications/{id}/approve", async (Guid id, HttpContext context, Vitrin.Auth.Infrastructure.Data.AuthDbContext db, Vitrin.Auth.Infrastructure.Kafka.IAuthNotificationPublisher notificationPublisher, Vitrin.Auth.Application.Interfaces.IAccountEmailService emailService, IAuditLogger auditLogger) =>
{
    var appToApprove = await db.MakerApplications.FindAsync(id);
    if (appToApprove == null) return Results.NotFound();
    
    appToApprove.Approve();
    
    var user = await db.Users.FindAsync(appToApprove.UserId);
    if (user != null)
    {
        user.UpdateRole(Vitrin.Auth.Domain.Entities.UserRole.Maker);
        await notificationPublisher.NotifyAsync(
            user.Id,
            "Tebrikler, artık Maker oldunuz!",
            "maker_approved",
            context.RequestAborted);
    }
    
    await db.SaveChangesAsync(context.RequestAborted);
    if (user != null)
        await emailService.SendMakerApprovedAsync(user, context.RequestAborted);

    await auditLogger.WriteAsync(
        new AuditEvent("admin.maker_application_approved", context.User.GetUserId(), "MakerApplication", id.ToString(), "Succeeded", context.TraceIdentifier),
        context.RequestAborted);
    return Results.Ok(new { Message = "Approved successfully." });
}).RequireAuthorization(VitrinAuthDefaults.AdminPolicy);

app.MapPost("/api/auth/admin/maker-applications/{id}/reject", async (Guid id, HttpContext context, Vitrin.Auth.Infrastructure.Data.AuthDbContext db, IAuditLogger auditLogger) =>
{
    var appToReject = await db.MakerApplications.FindAsync(id);
    if (appToReject == null) return Results.NotFound();
    
    appToReject.Reject();
    await db.SaveChangesAsync();
    await auditLogger.WriteAsync(
        new AuditEvent("admin.maker_application_rejected", context.User.GetUserId(), "MakerApplication", id.ToString(), "Succeeded", context.TraceIdentifier),
        context.RequestAborted);
    return Results.Ok(new { Message = "Rejected successfully." });
}).RequireAuthorization(VitrinAuthDefaults.AdminPolicy);

// MODERATION: REPORTS, BANS, APPEALS AND AUDIT LOG
app.MapPost("/api/auth/moderation/reports", async (
    HttpContext context,
    [Microsoft.AspNetCore.Mvc.FromBody] CreateModerationReportRequest request,
    Vitrin.Auth.Infrastructure.Data.AuthDbContext db,
    IAuditLogger auditLogger) =>
{
    var reporterUserId = context.User.GetUserId();
    if (reporterUserId is null) return Results.Unauthorized();
    if (!Enum.TryParse<Vitrin.Auth.Domain.Entities.ModerationTargetType>(request.TargetType, true, out var targetType))
        return ApiProblemResults.BadRequest("Unknown report target type.", "moderation.target_type_invalid");
    if (!Enum.TryParse<Vitrin.Auth.Domain.Entities.ReportCategory>(request.Category, true, out var category))
        return ApiProblemResults.BadRequest("Unknown report category.", "moderation.category_invalid");
    if (request.TargetOwnerUserId == reporterUserId)
        return ApiProblemResults.BadRequest("You cannot report your own content.", "moderation.self_report_not_allowed");

    var hasOpenDuplicate = await db.ModerationReports.AnyAsync(report =>
        report.ReporterUserId == reporterUserId.Value
        && report.TargetType == targetType
        && report.TargetId == request.TargetId
        && (report.Status == Vitrin.Auth.Domain.Entities.ModerationCaseStatus.Open
            || report.Status == Vitrin.Auth.Domain.Entities.ModerationCaseStatus.UnderReview));
    if (hasOpenDuplicate)
        return ApiProblemResults.BadRequest("You already have an open report for this target.", "moderation.duplicate_report");

    var reportResult = Vitrin.Auth.Domain.Entities.ModerationReport.Create(
        reporterUserId.Value,
        targetType,
        request.TargetId,
        request.TargetOwnerUserId,
        category,
        request.Details);
    if (!reportResult.IsSuccess)
        return ApiProblemResults.BadRequest(reportResult.Error, "moderation.report_invalid");

    db.ModerationReports.Add(reportResult.Value);
    await db.SaveChangesAsync(context.RequestAborted);
    await auditLogger.WriteAsync(
        new AuditEvent(
            "moderation.report_created",
            reporterUserId,
            targetType.ToString(),
            request.TargetId.ToString(),
            "Succeeded",
            context.TraceIdentifier,
            category.ToString()),
        context.RequestAborted);
    return Results.Ok(new { reportResult.Value.Id, reportResult.Value.Status });
}).RequireAuthorization();

app.MapGet("/api/auth/moderation/reports/me", async (
    HttpContext context,
    Vitrin.Auth.Infrastructure.Data.AuthDbContext db) =>
{
    var userId = context.User.GetUserId();
    if (userId is null) return Results.Unauthorized();
    var reports = await db.ModerationReports
        .AsNoTracking()
        .Where(report => report.ReporterUserId == userId.Value)
        .OrderByDescending(report => report.CreatedAtUtc)
        .Take(100)
        .ToListAsync();
    return Results.Ok(reports);
}).RequireAuthorization();

app.MapGet("/api/auth/moderation/appeals/me", async (
    HttpContext context,
    Vitrin.Auth.Infrastructure.Data.AuthDbContext db) =>
{
    var userId = context.User.GetUserId();
    if (userId is null) return Results.Unauthorized();
    var appeals = await db.ModerationAppeals
        .AsNoTracking()
        .Where(appeal => appeal.UserId == userId.Value)
        .OrderByDescending(appeal => appeal.CreatedAtUtc)
        .Take(100)
        .ToListAsync();
    return Results.Ok(appeals);
}).RequireAuthorization();

app.MapPost("/api/auth/moderation/appeals", async (
    HttpContext context,
    [Microsoft.AspNetCore.Mvc.FromBody] CreateAppealRequest request,
    Vitrin.Auth.Infrastructure.Data.AuthDbContext db,
    IAuditLogger auditLogger) =>
{
    var userId = context.User.GetUserId();
    if (userId is null) return Results.Unauthorized();
    var ban = await db.UserBans.FindAsync([request.BanId], context.RequestAborted);
    if (ban is null || ban.UserId != userId.Value || !ban.IsActive(DateTime.UtcNow))
        return ApiProblemResults.BadRequest("An active ban belonging to you is required.", "moderation.active_ban_required");
    if (await db.ModerationAppeals.AnyAsync(
            appeal => appeal.BanId == ban.Id && appeal.Status == Vitrin.Auth.Domain.Entities.AppealStatus.Open,
            context.RequestAborted))
        return ApiProblemResults.BadRequest("There is already an open appeal for this ban.", "moderation.duplicate_appeal");

    var appealResult = Vitrin.Auth.Domain.Entities.ModerationAppeal.Create(ban.Id, userId.Value, request.Statement);
    if (!appealResult.IsSuccess)
        return ApiProblemResults.BadRequest(appealResult.Error, "moderation.appeal_invalid");

    db.ModerationAppeals.Add(appealResult.Value);
    await db.SaveChangesAsync(context.RequestAborted);
    await auditLogger.WriteAsync(
        new AuditEvent("moderation.appeal_created", userId, "UserBan", ban.Id.ToString(), "Succeeded", context.TraceIdentifier),
        context.RequestAborted);
    return Results.Ok(new { appealResult.Value.Id, appealResult.Value.Status });
}).RequireAuthorization();

app.MapGet("/api/auth/admin/moderation/reports", async (
    string? status,
    Vitrin.Auth.Infrastructure.Data.AuthDbContext db) =>
{
    var query = db.ModerationReports.AsNoTracking();
    if (Enum.TryParse<Vitrin.Auth.Domain.Entities.ModerationCaseStatus>(status, true, out var parsedStatus))
        query = query.Where(report => report.Status == parsedStatus);

    var reports = await query
        .OrderByDescending(report => report.CreatedAtUtc)
        .Take(300)
        .Select(report => new
        {
            report.Id,
            report.ReporterUserId,
            ReporterUsername = db.Users.Where(user => user.Id == report.ReporterUserId).Select(user => user.Username).FirstOrDefault(),
            report.TargetType,
            report.TargetId,
            report.TargetOwnerUserId,
            TargetOwnerUsername = db.Users.Where(user => user.Id == report.TargetOwnerUserId).Select(user => user.Username).FirstOrDefault(),
            report.Category,
            report.Details,
            report.Status,
            report.CreatedAtUtc,
            report.Resolution,
            report.ReviewedAtUtc
        })
        .ToListAsync();
    return Results.Ok(reports);
}).RequireAuthorization(VitrinAuthDefaults.AdminPolicy);

app.MapPatch("/api/auth/admin/moderation/reports/{id:guid}", async (
    Guid id,
    HttpContext context,
    [Microsoft.AspNetCore.Mvc.FromBody] ResolveModerationReportRequest request,
    Vitrin.Auth.Infrastructure.Data.AuthDbContext db,
    Vitrin.Auth.Infrastructure.Kafka.IAuthNotificationPublisher notificationPublisher,
    IAuditLogger auditLogger) =>
{
    var moderatorUserId = context.User.GetUserId();
    if (moderatorUserId is null) return Results.Unauthorized();
    var report = await db.ModerationReports.FindAsync([id], context.RequestAborted);
    if (report is null) return ApiProblemResults.NotFound("Report not found.", "moderation.report_not_found");
    if (report.Status is Vitrin.Auth.Domain.Entities.ModerationCaseStatus.Resolved or Vitrin.Auth.Domain.Entities.ModerationCaseStatus.Dismissed)
        return ApiProblemResults.BadRequest("This report is already closed.", "moderation.report_already_closed");

    Guid? createdBanId = null;
    if (request.BanDays.HasValue && report.TargetOwnerUserId.HasValue)
    {
        var targetUser = await db.Users.FindAsync([report.TargetOwnerUserId.Value], context.RequestAborted);
        if (targetUser is not null && targetUser.Role != Vitrin.Auth.Domain.Entities.UserRole.Admin)
        {
            var expiresAtUtc = request.BanDays.Value > 0 ? DateTime.UtcNow.AddDays(request.BanDays.Value) : (DateTime?)null;
            var ban = Vitrin.Auth.Domain.Entities.UserBan.Create(
                targetUser.Id,
                moderatorUserId.Value,
                request.Resolution,
                expiresAtUtc);
            db.UserBans.Add(ban);
            targetUser.Suspend(ban.Id, request.Resolution, expiresAtUtc);
            createdBanId = ban.Id;
            await notificationPublisher.NotifyAsync(
                targetUser.Id,
                expiresAtUtc.HasValue
                    ? $"Hesabınız {expiresAtUtc.Value:dd.MM.yyyy HH:mm} tarihine kadar askıya alındı. Gerekçe: {request.Resolution}"
                    : $"Hesabınız kalıcı olarak askıya alındı. Gerekçe: {request.Resolution}",
                "account_banned",
                context.RequestAborted);
        }
    }

    report.Resolve(moderatorUserId.Value, request.Resolution, request.Dismissed);
    await db.SaveChangesAsync(context.RequestAborted);
    await auditLogger.WriteAsync(
        new AuditEvent(
            request.Dismissed ? "admin.report_dismissed" : "admin.report_resolved",
            moderatorUserId,
            report.TargetType.ToString(),
            report.TargetId.ToString(),
            "Succeeded",
            context.TraceIdentifier,
            $"ReportId={report.Id}; Resolution={request.Resolution}; BanId={createdBanId}"),
        context.RequestAborted);
    return Results.Ok(new { report.Id, report.Status, BanId = createdBanId });
}).RequireAuthorization(VitrinAuthDefaults.AdminPolicy);

app.MapPost("/api/auth/admin/moderation/bans", async (
    HttpContext context,
    [Microsoft.AspNetCore.Mvc.FromBody] CreateUserBanRequest request,
    Vitrin.Auth.Infrastructure.Data.AuthDbContext db,
    Vitrin.Auth.Infrastructure.Kafka.IAuthNotificationPublisher notificationPublisher,
    IAuditLogger auditLogger) =>
{
    var moderatorUserId = context.User.GetUserId();
    if (moderatorUserId is null) return Results.Unauthorized();
    if (request.UserId == moderatorUserId.Value)
        return ApiProblemResults.BadRequest("You cannot ban yourself.", "moderation.self_ban_not_allowed");
    if (string.IsNullOrWhiteSpace(request.Reason))
        return ApiProblemResults.BadRequest("A ban reason is required.", "moderation.ban_reason_required");

    var user = await db.Users.FindAsync([request.UserId], context.RequestAborted);
    if (user is null) return ApiProblemResults.NotFound("User not found.", "user.not_found");
    if (user.Role == Vitrin.Auth.Domain.Entities.UserRole.Admin)
        return ApiProblemResults.BadRequest("Administrators cannot be banned.", "moderation.admin_ban_not_allowed");
    if (user.IsBanned(DateTime.UtcNow))
        return ApiProblemResults.BadRequest("The user already has an active ban.", "moderation.active_ban_exists");

    var expiresAtUtc = request.DurationDays.HasValue && request.DurationDays.Value > 0
        ? DateTime.UtcNow.AddDays(request.DurationDays.Value)
        : (DateTime?)null;
    var ban = Vitrin.Auth.Domain.Entities.UserBan.Create(user.Id, moderatorUserId.Value, request.Reason, expiresAtUtc);
    db.UserBans.Add(ban);
    user.Suspend(ban.Id, request.Reason, expiresAtUtc);
    await notificationPublisher.NotifyAsync(
        user.Id,
        expiresAtUtc.HasValue
            ? $"Hesabınız {expiresAtUtc.Value:dd.MM.yyyy HH:mm} tarihine kadar askıya alındı. Gerekçe: {request.Reason}"
            : $"Hesabınız kalıcı olarak askıya alındı. Gerekçe: {request.Reason}",
        "account_banned",
        context.RequestAborted);
    await db.SaveChangesAsync(context.RequestAborted);
    await auditLogger.WriteAsync(
        new AuditEvent("admin.user_banned", moderatorUserId, "User", user.Id.ToString(), "Succeeded", context.TraceIdentifier, request.Reason),
        context.RequestAborted);
    return Results.Ok(new { ban.Id, ban.ExpiresAtUtc });
}).RequireAuthorization(VitrinAuthDefaults.AdminPolicy);

app.MapGet("/api/auth/admin/moderation/bans", async (Vitrin.Auth.Infrastructure.Data.AuthDbContext db) =>
{
    var now = DateTime.UtcNow;
    var bans = await db.UserBans
        .AsNoTracking()
        .Where(ban => !ban.RevokedAtUtc.HasValue && (!ban.ExpiresAtUtc.HasValue || ban.ExpiresAtUtc > now))
        .OrderByDescending(ban => ban.CreatedAtUtc)
        .Take(300)
        .Select(ban => new
        {
            ban.Id,
            ban.UserId,
            Username = db.Users.Where(user => user.Id == ban.UserId).Select(user => user.Username).FirstOrDefault(),
            ban.Reason,
            ban.CreatedAtUtc,
            ban.ExpiresAtUtc,
            ban.IssuedByUserId
        })
        .ToListAsync();
    return Results.Ok(bans);
}).RequireAuthorization(VitrinAuthDefaults.AdminPolicy);

app.MapDelete("/api/auth/admin/moderation/bans/{id:guid}", async (
    Guid id,
    HttpContext context,
    [Microsoft.AspNetCore.Mvc.FromBody] RevokeUserBanRequest request,
    Vitrin.Auth.Infrastructure.Data.AuthDbContext db,
    Vitrin.Auth.Infrastructure.Kafka.IAuthNotificationPublisher notificationPublisher,
    IAuditLogger auditLogger) =>
{
    var moderatorUserId = context.User.GetUserId();
    if (moderatorUserId is null) return Results.Unauthorized();
    var ban = await db.UserBans.FindAsync([id], context.RequestAborted);
    if (ban is null) return ApiProblemResults.NotFound("Ban not found.", "moderation.ban_not_found");
    var user = await db.Users.FindAsync([ban.UserId], context.RequestAborted);

    ban.Revoke(moderatorUserId.Value, request.Reason);
    if (user?.ActiveBanId == ban.Id) user.LiftSuspension();
    await notificationPublisher.NotifyAsync(
        ban.UserId,
        $"Hesap askınız kaldırıldı. Not: {request.Reason}",
        "account_ban_revoked",
        context.RequestAborted);
    await db.SaveChangesAsync(context.RequestAborted);
    await auditLogger.WriteAsync(
        new AuditEvent("admin.user_ban_revoked", moderatorUserId, "UserBan", ban.Id.ToString(), "Succeeded", context.TraceIdentifier, request.Reason),
        context.RequestAborted);
    return Results.Ok(new { ban.Id, Revoked = true });
}).RequireAuthorization(VitrinAuthDefaults.AdminPolicy);

app.MapGet("/api/auth/admin/moderation/appeals", async (Vitrin.Auth.Infrastructure.Data.AuthDbContext db) =>
{
    var appeals = await db.ModerationAppeals
        .AsNoTracking()
        .OrderByDescending(appeal => appeal.CreatedAtUtc)
        .Take(300)
        .Select(appeal => new
        {
            appeal.Id,
            appeal.BanId,
            appeal.UserId,
            Username = db.Users.Where(user => user.Id == appeal.UserId).Select(user => user.Username).FirstOrDefault(),
            appeal.Statement,
            appeal.Status,
            appeal.CreatedAtUtc,
            appeal.ReviewNote,
            appeal.ReviewedAtUtc
        })
        .ToListAsync();
    return Results.Ok(appeals);
}).RequireAuthorization(VitrinAuthDefaults.AdminPolicy);

app.MapPatch("/api/auth/admin/moderation/appeals/{id:guid}", async (
    Guid id,
    HttpContext context,
    [Microsoft.AspNetCore.Mvc.FromBody] ReviewAppealRequest request,
    Vitrin.Auth.Infrastructure.Data.AuthDbContext db,
    Vitrin.Auth.Infrastructure.Kafka.IAuthNotificationPublisher notificationPublisher,
    IAuditLogger auditLogger) =>
{
    var moderatorUserId = context.User.GetUserId();
    if (moderatorUserId is null) return Results.Unauthorized();
    var appeal = await db.ModerationAppeals.FindAsync([id], context.RequestAborted);
    if (appeal is null) return ApiProblemResults.NotFound("Appeal not found.", "moderation.appeal_not_found");
    if (appeal.Status != Vitrin.Auth.Domain.Entities.AppealStatus.Open)
        return ApiProblemResults.BadRequest("This appeal is already reviewed.", "moderation.appeal_already_reviewed");

    var ban = await db.UserBans.FindAsync([appeal.BanId], context.RequestAborted);
    var user = await db.Users.FindAsync([appeal.UserId], context.RequestAborted);
    appeal.Review(moderatorUserId.Value, request.Approved, request.Note);
    if (request.Approved && ban is not null)
    {
        ban.Revoke(moderatorUserId.Value, $"Appeal approved: {request.Note}");
        if (user?.ActiveBanId == ban.Id) user.LiftSuspension();
    }

    await notificationPublisher.NotifyAsync(
        appeal.UserId,
        request.Approved
            ? $"İtirazınız kabul edildi ve hesap askınız kaldırıldı. Not: {request.Note}"
            : $"İtirazınız reddedildi. Not: {request.Note}",
        request.Approved ? "appeal_approved" : "appeal_rejected",
        context.RequestAborted);
    await db.SaveChangesAsync(context.RequestAborted);
    await auditLogger.WriteAsync(
        new AuditEvent(
            request.Approved ? "admin.appeal_approved" : "admin.appeal_rejected",
            moderatorUserId,
            "ModerationAppeal",
            appeal.Id.ToString(),
            "Succeeded",
            context.TraceIdentifier,
            request.Note),
        context.RequestAborted);
    return Results.Ok(new { appeal.Id, appeal.Status });
}).RequireAuthorization(VitrinAuthDefaults.AdminPolicy);

app.MapGet("/api/auth/admin/moderation/audit", async (
    string? action,
    string? resourceType,
    Vitrin.Auth.Infrastructure.Data.AuthDbContext db) =>
{
    var query = db.ModerationAuditEntries.AsNoTracking();
    if (!string.IsNullOrWhiteSpace(action)) query = query.Where(entry => entry.Action == action);
    if (!string.IsNullOrWhiteSpace(resourceType)) query = query.Where(entry => entry.ResourceType == resourceType);
    var entries = await query
        .OrderByDescending(entry => entry.OccurredAtUtc)
        .Take(500)
        .ToListAsync();
    return Results.Ok(entries);
}).RequireAuthorization(VitrinAuthDefaults.AdminPolicy);

app.MapGet("/api/auth/activity", async (int? limit, Vitrin.Auth.Infrastructure.Data.AuthDbContext db) =>
{
    var take = Math.Clamp(limit ?? 30, 1, 100);
    var activity = await (
        from follow in db.UserFollows.AsNoTracking()
        join actor in db.Users.AsNoTracking() on follow.FollowerId equals actor.Id
        join followed in db.Users.AsNoTracking() on follow.FollowingId equals followed.Id
        orderby follow.CreatedAt descending
        select new AuthActivityResponse(
            $"follow:{follow.FollowerId}:{follow.FollowingId}",
            "follow",
            actor.Id,
            actor.Username,
            $"@{followed.Username} kullanıcısını takip etmeye başladı",
            "User",
            followed.Id,
            followed.Username,
            follow.CreatedAt))
        .Take(take)
        .ToListAsync();
    return Results.Ok(activity);
});

// FOLLOW SYSTEM
app.MapPost("/api/auth/users/{username}/follow", async (string username, HttpContext context, Vitrin.Auth.Infrastructure.Data.AuthDbContext db, Vitrin.Auth.Infrastructure.Kafka.IAuthNotificationPublisher notificationPublisher) =>
{
    var followerId = context.User.GetUserId();
    if (followerId == null) return Results.Unauthorized();

    var normalizedUsername = username.Trim();
    var userToFollow = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.FirstOrDefaultAsync(db.Users, u => u.Username == normalizedUsername);
    if (userToFollow == null) return Results.NotFound();
    if (userToFollow.Id == followerId.Value) return ApiProblemResults.BadRequest("You cannot follow yourself.", "follow.self_not_allowed");

    var existingFollow = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.FirstOrDefaultAsync(db.UserFollows, uf => uf.FollowerId == followerId.Value && uf.FollowingId == userToFollow.Id);
    if (existingFollow != null) return Results.Ok(new { Message = "Already following." });

    db.UserFollows.Add(new Vitrin.Auth.Domain.Entities.UserFollow(followerId.Value, userToFollow.Id));

    var followerUser = await db.Users.FindAsync(followerId.Value);
    await notificationPublisher.NotifyAsync(
        userToFollow.Id,
        $"@{followerUser?.Username} sizi takip etmeye başladı.",
        "new_follower",
        context.RequestAborted);
    await db.SaveChangesAsync(context.RequestAborted);

    return Results.Ok(new { Message = "Followed successfully." });
}).RequireAuthorization();

app.MapDelete("/api/auth/users/{username}/follow", async (string username, HttpContext context, Vitrin.Auth.Infrastructure.Data.AuthDbContext db) =>
{
    var followerId = context.User.GetUserId();
    if (followerId == null) return Results.Unauthorized();

    var normalizedUsername = username.Trim();
    var userToUnfollow = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.FirstOrDefaultAsync(db.Users, u => u.Username == normalizedUsername);
    if (userToUnfollow == null) return Results.NotFound();

    var existingFollow = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.FirstOrDefaultAsync(db.UserFollows, uf => uf.FollowerId == followerId.Value && uf.FollowingId == userToUnfollow.Id);
    if (existingFollow == null) return Results.Ok(new { Message = "Not following." });

    db.UserFollows.Remove(existingFollow);
    await db.SaveChangesAsync();

    return Results.Ok(new { Message = "Unfollowed successfully." });
}).RequireAuthorization();

app.MapGet("/api/auth/users/{username}/followers", async (string username, Vitrin.Auth.Infrastructure.Data.AuthDbContext db) =>
{
    var normalizedUsername = username.Trim();
    var user = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.FirstOrDefaultAsync(db.Users.AsNoTracking(), u => u.Username == normalizedUsername);
    if (user == null) return Results.NotFound();

    var followers = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.ToListAsync(
        db.UserFollows
          .AsNoTracking()
          .Where(uf => uf.FollowingId == user.Id)
          .OrderByDescending(uf => uf.CreatedAt)
          .Take(500)
          .Select(uf => new { 
              uf.Follower.Id, 
              uf.Follower.Username, 
              uf.Follower.FullName, 
              uf.Follower.AvatarUrl,
              uf.Follower.Headline
          })
    );

    return Results.Ok(followers);
});

app.MapGet("/api/auth/users/{username}/following", async (string username, Vitrin.Auth.Infrastructure.Data.AuthDbContext db) =>
{
    var normalizedUsername = username.Trim();
    var user = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.FirstOrDefaultAsync(db.Users.AsNoTracking(), u => u.Username == normalizedUsername);
    if (user == null) return Results.NotFound();

    var following = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.ToListAsync(
        db.UserFollows
          .AsNoTracking()
          .Where(uf => uf.FollowerId == user.Id)
          .OrderByDescending(uf => uf.CreatedAt)
          .Take(500)
          .Select(uf => new { 
              uf.Following.Id, 
              uf.Following.Username, 
              uf.Following.FullName, 
              uf.Following.AvatarUrl,
              uf.Following.Headline
          })
    );

    return Results.Ok(following);
});

app.MapGet("/api/auth/users/{userId}/followers-ids", async (Guid userId, Vitrin.Auth.Infrastructure.Data.AuthDbContext db) =>
{
    var followerIds = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.ToListAsync(
        db.UserFollows
          .AsNoTracking()
          .Where(uf => uf.FollowingId == userId)
          .Take(5_000)
          .Select(uf => uf.FollowerId)
    );

    return Results.Ok(followerIds);
});

// Gamification
app.MapPost("/api/auth/users/me/record-vote", async (HttpContext context, Vitrin.Auth.Infrastructure.Data.AuthDbContext db) =>
{
    var userId = context.User.GetUserId();
    if (userId is null) return Results.Unauthorized();

    var user = await db.Users.FindAsync(userId.Value);
    if (user == null) return Results.NotFound();

    user.RecordVoteActivity();

    // Otomatik rozet kontrolleri
    if (user.CurrentStreak >= 5)
    {
        user.AddBadge("Ateşli Avcı", "Flame");
    }
    if (user.LongestStreak >= 30)
    {
        user.AddBadge("Topluluk Lideri", "Award");
    }

    await db.SaveChangesAsync();

    return Results.Ok(new { 
        CurrentStreak = user.CurrentStreak, 
        LongestStreak = user.LongestStreak 
    });
}).RequireAuthorization();

// Gamification - Leaderboard
app.MapGet("/api/auth/leaderboard", async (Vitrin.Auth.Infrastructure.Data.AuthDbContext db) =>
{
    var topStreaks = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.ToListAsync(
        db.Users
          .AsNoTracking()
          .OrderByDescending(u => u.CurrentStreak)
          .Take(10)
          .Select(u => new { u.Id, u.Username, u.FullName, u.AvatarUrl, u.Headline, u.CurrentStreak })
    );

    // SQL'de Follower sayısını hesaplayarak sıralamak için Follower sistemini kullanıyoruz.
    var topMakers = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.ToListAsync(
        db.Users
          .AsNoTracking()
          .OrderByDescending(u => db.UserFollows.Count(f => f.FollowingId == u.Id))
          .Take(10)
          .Select(u => new { 
              u.Id, 
              u.Username, 
              u.FullName, 
              u.AvatarUrl, 
              u.Headline, 
              FollowerCount = db.UserFollows.Count(f => f.FollowingId == u.Id)
          })
    );

    return Results.Ok(new {
        TopStreaks = topStreaks,
        TopMakers = topMakers
    });
});

// ─── KVKK: Veri Export & Hesap Silme ──────────────────────────────────────

// Kullanıcının kendi verisini JSON olarak export et
app.MapGet("/api/auth/users/me/data-export", async (
    HttpContext context,
    Vitrin.Auth.Infrastructure.Data.AuthDbContext db) =>
{
    var userId = context.User.GetUserId();
    if (userId is null) return Results.Unauthorized();

    var user = await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId.Value, context.RequestAborted);
    if (user is null) return Results.NotFound();

    var export = new
    {
        ExportedAt = DateTime.UtcNow,
        Profile = new
        {
            user.Id, user.Email, user.Username, user.FullName,
            user.Headline, user.About, user.WebsiteUrl, user.GithubUrl,
            user.LinkedInUrl, user.Role, user.CreatedAt, user.Provider
        },
        Badges = await db.UserBadges.AsNoTracking()
            .Where(b => b.UserId == userId.Value)
            .Select(b => new { b.Name, b.Icon, b.EarnedAt })
            .ToListAsync(context.RequestAborted),
        Follows = new
        {
            Following = await db.UserFollows.AsNoTracking()
                .Where(f => f.FollowerId == userId.Value)
                .Select(f => f.FollowingId)
                .ToListAsync(context.RequestAborted),
            Followers = await db.UserFollows.AsNoTracking()
                .Where(f => f.FollowingId == userId.Value)
                .Select(f => f.FollowerId)
                .ToListAsync(context.RequestAborted)
        }
    };

    return Results.Json(export);
})
.WithName("ExportMyData")
.RequireAuthorization();

// Hesap silme talebi başlat (30 gün sonra anonim hale gelir)
app.MapPost("/api/auth/users/me/request-deletion", async (
    HttpContext context,
    Vitrin.Auth.Infrastructure.Data.AuthDbContext db,
    IAuditLogger auditLogger) =>
{
    var userId = context.User.GetUserId();
    if (userId is null) return Results.Unauthorized();

    var user = await db.Users.FindAsync([userId.Value], context.RequestAborted);
    if (user is null) return Results.NotFound();
    if (user.IsAnonymized)
        return ApiProblemResults.BadRequest("Hesap zaten silinmiş.", "user.already_deleted");

    user.RequestDeletion(DateTime.UtcNow);
    await db.SaveChangesAsync(context.RequestAborted);

    await auditLogger.WriteAsync(
        new AuditEvent("user.deletion_requested", userId, "User", userId.ToString(), "Succeeded", context.TraceIdentifier),
        context.RequestAborted);

    return Results.Ok(new
    {
        Message = "Hesap silme talebiniz alındı. Verileriniz 30 gün içinde anonim hale getirilecek.",
        ScheduledDeletionAt = DateTime.UtcNow.AddDays(30)
    });
})
.WithName("RequestAccountDeletion")
.RequireAuthorization();

// Hesap silme talebini geri al
app.MapDelete("/api/auth/users/me/request-deletion", async (
    HttpContext context,
    Vitrin.Auth.Infrastructure.Data.AuthDbContext db,
    IAuditLogger auditLogger) =>
{
    var userId = context.User.GetUserId();
    if (userId is null) return Results.Unauthorized();

    var user = await db.Users.FindAsync([userId.Value], context.RequestAborted);
    if (user is null) return Results.NotFound();
    if (!user.DeleteRequestedAtUtc.HasValue)
        return ApiProblemResults.BadRequest("Bekleyen bir silme talebi yok.", "user.no_pending_deletion");

    user.CancelDeletion();
    await db.SaveChangesAsync(context.RequestAborted);

    await auditLogger.WriteAsync(
        new AuditEvent("user.deletion_cancelled", userId, "User", userId.ToString(), "Succeeded", context.TraceIdentifier),
        context.RequestAborted);

    return Results.Ok(new { Message = "Hesap silme talebi iptal edildi." });
})
.WithName("CancelAccountDeletion")
.RequireAuthorization();

// ─── Feature Flags ─────────────────────────────────────────────────────────

// Tüm aktif flag'leri listele (public — frontend kullanır)
app.MapGet("/api/feature-flags", async (
    HttpContext context,
    Vitrin.Auth.Infrastructure.Data.AuthDbContext db) =>
{
    var userId = context.User.GetUserId();
    var userRole = context.User.FindFirst("Role")?.Value ?? context.User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value ?? "";

    var flags = await db.FeatureFlags
        .AsNoTracking()
        .Where(f => f.IsEnabled)
        .ToListAsync(context.RequestAborted);

    var result = flags
        .Where(f =>
        {
            var roles = f.GetAllowedRoles();
            if (roles.Count > 0 && !roles.Contains(userRole, StringComparer.OrdinalIgnoreCase))
                return false;
            return f.IsActiveForUser(userId);
        })
        .Select(f => new
        {
            f.Key,
            f.VariantPayload,
            IsActive = true
        })
        .ToList();

    return Results.Ok(result);
})
.WithName("GetActiveFeatureFlags");

// Admin: tüm flag'leri yönet
app.MapGet("/api/admin/feature-flags", async (Vitrin.Auth.Infrastructure.Data.AuthDbContext db) =>
{
    var flags = await db.FeatureFlags.AsNoTracking()
        .OrderBy(f => f.Key)
        .ToListAsync();
    return Results.Ok(flags);
})
.WithName("AdminGetFeatureFlags")
.RequireAuthorization(VitrinAuthDefaults.AdminPolicy);

app.MapPost("/api/admin/feature-flags", async (
    [Microsoft.AspNetCore.Mvc.FromBody] UpsertFeatureFlagRequest request,
    HttpContext context,
    Vitrin.Auth.Infrastructure.Data.AuthDbContext db,
    IAuditLogger auditLogger) =>
{
    var adminId = context.User.GetUserId();
    if (adminId is null) return Results.Unauthorized();

    var key = request.Key.Trim().ToLowerInvariant();
    var existing = await db.FeatureFlags.FirstOrDefaultAsync(f => f.Key == key, context.RequestAborted);

    if (existing is null)
    {
        var flag = Vitrin.Auth.Domain.Entities.FeatureFlag.Create(
            key, request.Description, request.IsEnabled,
            request.RolloutPercentage, request.AllowedRoles, request.VariantPayload);
        db.FeatureFlags.Add(flag);
    }
    else
    {
        existing.Update(request.Description, request.IsEnabled, request.RolloutPercentage,
            request.AllowedRoles, request.VariantPayload, adminId.Value);
    }

    await db.SaveChangesAsync(context.RequestAborted);
    await auditLogger.WriteAsync(
        new AuditEvent("admin.feature_flag_upserted", adminId, "FeatureFlag", key, "Succeeded", context.TraceIdentifier),
        context.RequestAborted);

    return Results.Ok(new { Key = key, Message = "Feature flag kaydedildi." });
})
.WithName("AdminUpsertFeatureFlag")
.RequireAuthorization(VitrinAuthDefaults.AdminPolicy);

app.MapDelete("/api/admin/feature-flags/{key}", async (
    string key,
    HttpContext context,
    Vitrin.Auth.Infrastructure.Data.AuthDbContext db,
    IAuditLogger auditLogger) =>
{
    var adminId = context.User.GetUserId();
    if (adminId is null) return Results.Unauthorized();

    var normalizedKey = key.Trim().ToLowerInvariant();
    var flag = await db.FeatureFlags.FirstOrDefaultAsync(f => f.Key == normalizedKey, context.RequestAborted);
    if (flag is null) return Results.NotFound();

    db.FeatureFlags.Remove(flag);
    await db.SaveChangesAsync(context.RequestAborted);
    await auditLogger.WriteAsync(
        new AuditEvent("admin.feature_flag_deleted", adminId, "FeatureFlag", normalizedKey, "Succeeded", context.TraceIdentifier),
        context.RequestAborted);

    return Results.NoContent();
})
.WithName("AdminDeleteFeatureFlag")
.RequireAuthorization(VitrinAuthDefaults.AdminPolicy);

// ─── Admin: KVKK Pending Deletions ─────────────────────────────────────────

// Silme bekleyen kullanıcılar + özet istatistikler
app.MapGet("/api/auth/admin/kvkk/pending-deletions", async (
    Vitrin.Auth.Infrastructure.Data.AuthDbContext db,
    CancellationToken ct) =>
{
    var now = DateTime.UtcNow;

    var pendingUsers = await db.Users
        .AsNoTracking()
        .Where(u => u.DeleteRequestedAtUtc.HasValue && !u.AnonymizedAtUtc.HasValue)
        .OrderBy(u => u.DeleteRequestedAtUtc)
        .Take(500)
        .Select(u => new
        {
            u.Id,
            u.Username,
            u.Email,
            u.FullName,
            u.DeleteRequestedAtUtc
        })
        .ToListAsync(ct);

    var users = pendingUsers.Select(u =>
    {
        var scheduled = u.DeleteRequestedAtUtc!.Value.AddDays(30);
        var remaining = (int)Math.Ceiling((scheduled - now).TotalDays);
        return new
        {
            u.Id,
            u.Username,
            u.Email,
            u.FullName,
            DeleteRequestedAtUtc = u.DeleteRequestedAtUtc,
            ScheduledDeletionAt = scheduled,
            DaysRemaining = Math.Max(0, remaining),
            IsOverdue = scheduled <= now
        };
    }).ToList();

    var firstOfMonth = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
    var anonymizedThisMonth = await db.Users
        .AsNoTracking()
        .CountAsync(u => u.AnonymizedAtUtc.HasValue && u.AnonymizedAtUtc.Value >= firstOfMonth, ct);
    var anonymizedTotal = await db.Users
        .AsNoTracking()
        .CountAsync(u => u.AnonymizedAtUtc.HasValue, ct);

    var stats = new
    {
        TotalPendingDeletions = users.Count,
        OverdueDeletions = users.Count(u => u.IsOverdue),
        AnonymizedThisMonth = anonymizedThisMonth,
        AnonymizedTotal = anonymizedTotal
    };

    return Results.Ok(new { users, stats });
})
.WithName("AdminGetKvkkPendingDeletions")
.RequireAuthorization(VitrinAuthDefaults.AdminPolicy);

// Bekleyen silme taleplerini CSV olarak export et
app.MapGet("/api/auth/admin/kvkk/pending-deletions/export", async (
    Vitrin.Auth.Infrastructure.Data.AuthDbContext db,
    CancellationToken ct) =>
{
    var now = DateTime.UtcNow;
    var pending = await db.Users
        .AsNoTracking()
        .Where(u => u.DeleteRequestedAtUtc.HasValue && !u.AnonymizedAtUtc.HasValue)
        .OrderBy(u => u.DeleteRequestedAtUtc)
        .Select(u => new { u.Id, u.Username, u.Email, u.FullName, u.DeleteRequestedAtUtc })
        .ToListAsync(ct);

    var lines = new System.Text.StringBuilder();
    lines.AppendLine("Id,Username,Email,FullName,DeleteRequestedAtUtc,ScheduledDeletionAt,IsOverdue");
    foreach (var u in pending)
    {
        var scheduled = u.DeleteRequestedAtUtc!.Value.AddDays(30);
        lines.AppendLine($"{u.Id},{u.Username},{u.Email},{u.FullName},{u.DeleteRequestedAtUtc:u},{scheduled:u},{scheduled <= now}");
    }

    var bytes = System.Text.Encoding.UTF8.GetBytes(lines.ToString());
    return Results.File(bytes, "text/csv", $"kvkk-pending-{now:yyyyMMdd}.csv");
})
.WithName("AdminExportKvkkPendingDeletions")
.RequireAuthorization(VitrinAuthDefaults.AdminPolicy);

app.Run();

// ─── Admin: KVKK Pending Deletions ─────────────────────────────────────────

// Bu endpoint'ler app.Run()'dan önce olmalı, ancak file sonuna da eklenebilir.
// Not: app.Run() öncesinde tanımlandıklarından çalışırlar.

public record MakerApplicationRequest(string PortfolioUrl, string Reason);
public record EmailTokenRequest(string Token);
public record EmailAddressRequest(string Email);
public record ResetPasswordRequest(string Token, string Password);
public record UpdateProfileRequest(string FullName, string Username, string? Headline, string? About, string? AvatarUrl, string? WebsiteUrl, string? GithubUrl, string? LinkedInUrl);
public record CreateModerationReportRequest(
    string TargetType,
    Guid TargetId,
    Guid? TargetOwnerUserId,
    string Category,
    string Details);
public record ResolveModerationReportRequest(string Resolution, bool Dismissed, int? BanDays = null);
public record CreateUserBanRequest(Guid UserId, string Reason, int? DurationDays = null);
public record RevokeUserBanRequest(string Reason);
public record CreateAppealRequest(Guid BanId, string Statement);
public record ReviewAppealRequest(bool Approved, string Note);
public record AuthActivityResponse(
    string Id,
    string Type,
    Guid ActorUserId,
    string ActorUsername,
    string Summary,
    string EntityType,
    Guid EntityId,
    string? Metadata,
    DateTime CreatedAtUtc);

public record UpsertFeatureFlagRequest(
    string Key,
    string Description,
    bool IsEnabled,
    int RolloutPercentage = 100,
    string? AllowedRoles = null,
    string? VariantPayload = null);
