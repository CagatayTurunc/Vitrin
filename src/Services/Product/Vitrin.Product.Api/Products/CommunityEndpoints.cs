using Microsoft.EntityFrameworkCore;
using Vitrin.Product.Domain.Entities;
using Vitrin.Product.Infrastructure.Data;
using Vitrin.Shared.Infrastructure.Api;
using Vitrin.Shared.Infrastructure.Auth;
using Vitrin.Shared.Kernel.Text;

namespace Vitrin.Product.Api.Products;

public static class CommunityEndpoints
{
    public static void MapCommunityEndpoints(this WebApplication app)
    {
        app.MapGet("/api/community/threads", GetThreads);
        app.MapGet("/api/community/threads/{slug}", GetThread);
        app.MapPost("/api/community/threads", CreateThread).RequireAuthorization();
        app.MapPost("/api/community/threads/{threadId:guid}/replies", CreateReply).RequireAuthorization();
        app.MapPut("/api/community/threads/{threadId:guid}/reaction", ToggleThreadReaction).RequireAuthorization();
        app.MapPut("/api/community/replies/{replyId:guid}/reaction", ToggleReplyReaction).RequireAuthorization();
        app.MapPut("/api/community/threads/{threadId:guid}/follow", FollowThread).RequireAuthorization();
        app.MapDelete("/api/community/threads/{threadId:guid}/follow", UnfollowThread).RequireAuthorization();
        app.MapPost("/api/community/threads/{threadId:guid}/report", ReportThread).RequireAuthorization();
        app.MapPatch("/api/community/threads/{threadId:guid}/moderation", ModerateThread).RequireAuthorization();
    }

    private static async Task<IResult> GetThreads(
        HttpContext context,
        ProductDbContext db,
        Guid? productId,
        CommunityThreadCategory? category,
        string? sort,
        int? take)
    {
        var userId = context.User.GetUserId();
        var query = db.CommunityThreads.AsNoTracking().AsQueryable();
        if (productId is not null) query = query.Where(item => item.ProductId == productId);
        if (category is not null) query = query.Where(item => item.Category == category);
        query = sort?.ToLowerInvariant() switch
        {
            "top" => query.OrderByDescending(item => db.CommunityReactions.Count(r => r.ThreadId == item.Id))
                .ThenByDescending(item => item.CreatedAtUtc),
            "trending" => query.OrderByDescending(item =>
                db.CommunityReplies.Count(reply => reply.ThreadId == item.Id) * 3 +
                db.CommunityReactions.Count(reaction => reaction.ThreadId == item.Id) * 2 + item.ViewCount)
                .ThenByDescending(item => item.CreatedAtUtc),
            _ => query.OrderByDescending(item => item.IsPinned).ThenByDescending(item => item.CreatedAtUtc)
        };

        var items = await query.Take(Math.Clamp(take ?? 40, 1, 100))
            .Select(item => new CommunityThreadSummaryResponse(
                item.Id, item.ProductId, item.AuthorId, item.Title, item.Slug, item.Body,
                item.Category, item.Kind, item.IsPinned, item.IsLocked, item.ViewCount,
                db.CommunityReplies.Count(reply => reply.ThreadId == item.Id),
                db.CommunityReactions.Count(reaction => reaction.ThreadId == item.Id),
                db.CommunityThreadFollows.Count(follow => follow.ThreadId == item.Id),
                userId != null && db.CommunityReactions.Any(reaction => reaction.ThreadId == item.Id && reaction.UserId == userId),
                userId != null && db.CommunityThreadFollows.Any(follow => follow.ThreadId == item.Id && follow.UserId == userId),
                item.CreatedAtUtc, item.UpdatedAtUtc))
            .ToListAsync(context.RequestAborted);
        return Results.Ok(items);
    }

    private static async Task<IResult> GetThread(string slug, HttpContext context, ProductDbContext db)
    {
        var thread = await db.CommunityThreads.FirstOrDefaultAsync(item => item.Slug == slug, context.RequestAborted);
        if (thread is null) return ApiProblemResults.NotFound("Thread not found.", "community.thread_not_found");
        thread.RecordView();
        await db.SaveChangesAsync(context.RequestAborted);
        var userId = context.User.GetUserId();
        var reactions = await db.CommunityReactions.AsNoTracking().Where(item => item.ThreadId == thread.Id ||
            db.CommunityReplies.Where(reply => reply.ThreadId == thread.Id).Select(reply => reply.Id).Contains(item.ReplyId!.Value)).ToListAsync(context.RequestAborted);
        var replyEntities = await db.CommunityReplies.AsNoTracking().Where(item => item.ThreadId == thread.Id)
            .OrderBy(item => item.CreatedAtUtc)
            .ToListAsync(context.RequestAborted);
        var replies = replyEntities.Select(item => new CommunityReplyResponse(
            item.Id, item.AuthorId, item.ParentReplyId, item.Body, item.IsOfficial,
            reactions.Count(reaction => reaction.ReplyId == item.Id),
            userId != null && reactions.Any(reaction => reaction.ReplyId == item.Id && reaction.UserId == userId),
            item.CreatedAtUtc, item.EditedAtUtc)).ToList();
        var response = new CommunityThreadDetailsResponse(
            thread.Id, thread.ProductId, thread.AuthorId, thread.Title, thread.Slug, thread.Body,
            thread.Category, thread.Kind, thread.IsPinned, thread.IsLocked, thread.ViewCount,
            replies.Count, reactions.Count(item => item.ThreadId == thread.Id),
            await db.CommunityThreadFollows.CountAsync(item => item.ThreadId == thread.Id, context.RequestAborted),
            userId != null && reactions.Any(item => item.ThreadId == thread.Id && item.UserId == userId),
            userId != null && await db.CommunityThreadFollows.AnyAsync(item => item.ThreadId == thread.Id && item.UserId == userId, context.RequestAborted),
            thread.CreatedAtUtc, thread.UpdatedAtUtc, replies);
        return Results.Ok(response);
    }

    private static async Task<IResult> CreateThread(CreateCommunityThreadRequest request, HttpContext context, ProductDbContext db)
    {
        var userId = context.User.GetUserId();
        if (userId is null) return Results.Unauthorized();
        if (request.ProductId is { } productId && !await db.Products.AnyAsync(item => item.Id == productId && item.Status == ProductStatus.Published, context.RequestAborted))
            return ApiProblemResults.NotFound("Product not found.", "product.not_found");
        var baseSlug = SlugGenerator.Generate(request.Title);
        var slug = baseSlug;
        var suffix = 2;
        while (await db.CommunityThreads.AnyAsync(item => item.Slug == slug, context.RequestAborted)) slug = $"{baseSlug}-{suffix++}";
        var result = CommunityThread.Create(userId.Value, request.ProductId, request.Title, slug, request.Body, request.Category, request.Kind);
        if (!result.IsSuccess) return ApiProblemResults.BadRequest(result.Error, "community.invalid_thread");
        db.CommunityThreads.Add(result.Value!);
        await db.SaveChangesAsync(context.RequestAborted);
        return Results.Created($"/api/community/threads/{slug}", new { result.Value!.Id, result.Value.Slug });
    }

    private static async Task<IResult> CreateReply(Guid threadId, CreateCommunityReplyRequest request, HttpContext context, ProductDbContext db)
    {
        var userId = context.User.GetUserId();
        if (userId is null) return Results.Unauthorized();
        var thread = await db.CommunityThreads.AsNoTracking().FirstOrDefaultAsync(item => item.Id == threadId, context.RequestAborted);
        if (thread is null) return ApiProblemResults.NotFound("Thread not found.", "community.thread_not_found");
        if (thread.IsLocked) return ApiProblemResults.BadRequest("Thread is locked.", "community.thread_locked");
        var isOfficial = thread.ProductId is { } productId && await db.Products.AnyAsync(item => item.Id == productId &&
            (item.MakerId == userId || item.TeamMembers.Any(member => member.UserId == userId)), context.RequestAborted);
        var result = CommunityReply.Create(threadId, userId.Value, request.ParentReplyId, request.Body, isOfficial);
        if (!result.IsSuccess) return ApiProblemResults.BadRequest(result.Error, "community.invalid_reply");
        db.CommunityReplies.Add(result.Value!);
        await db.SaveChangesAsync(context.RequestAborted);
        return Results.Ok(new { result.Value!.Id, result.Value.CreatedAtUtc, result.Value.IsOfficial });
    }

    private static async Task<IResult> ToggleThreadReaction(Guid threadId, HttpContext context, ProductDbContext db) =>
        await ToggleReaction(context.User.GetUserId(), threadId, null, db, context.RequestAborted);
    private static async Task<IResult> ToggleReplyReaction(Guid replyId, HttpContext context, ProductDbContext db) =>
        await ToggleReaction(context.User.GetUserId(), null, replyId, db, context.RequestAborted);

    private static async Task<IResult> ToggleReaction(Guid? userId, Guid? threadId, Guid? replyId, ProductDbContext db, CancellationToken cancellationToken)
    {
        if (userId is null) return Results.Unauthorized();
        var existing = await db.CommunityReactions.FirstOrDefaultAsync(item => item.UserId == userId && item.ThreadId == threadId && item.ReplyId == replyId, cancellationToken);
        if (existing is null) db.CommunityReactions.Add(threadId is { } id ? CommunityReaction.ForThread(userId.Value, id) : CommunityReaction.ForReply(userId.Value, replyId!.Value));
        else db.CommunityReactions.Remove(existing);
        await db.SaveChangesAsync(cancellationToken);
        var count = await db.CommunityReactions.CountAsync(item => item.ThreadId == threadId && item.ReplyId == replyId, cancellationToken);
        return Results.Ok(new { IsReacted = existing is null, Count = count });
    }

    private static async Task<IResult> FollowThread(Guid threadId, HttpContext context, ProductDbContext db)
    {
        var userId = context.User.GetUserId(); if (userId is null) return Results.Unauthorized();
        if (!await db.CommunityThreadFollows.AnyAsync(item => item.ThreadId == threadId && item.UserId == userId, context.RequestAborted))
        { db.CommunityThreadFollows.Add(CommunityThreadFollow.Create(userId.Value, threadId)); await db.SaveChangesAsync(context.RequestAborted); }
        return Results.NoContent();
    }
    private static async Task<IResult> UnfollowThread(Guid threadId, HttpContext context, ProductDbContext db)
    {
        var userId = context.User.GetUserId(); if (userId is null) return Results.Unauthorized();
        await db.CommunityThreadFollows.Where(item => item.ThreadId == threadId && item.UserId == userId).ExecuteDeleteAsync(context.RequestAborted);
        return Results.NoContent();
    }
    private static async Task<IResult> ReportThread(Guid threadId, ReportCommunityThreadRequest request, HttpContext context, ProductDbContext db)
    {
        var userId = context.User.GetUserId(); if (userId is null) return Results.Unauthorized();
        if (string.IsNullOrWhiteSpace(request.Reason)) return ApiProblemResults.BadRequest("Reason is required.", "community.report_reason_required");
        if (!await db.CommunityReports.AnyAsync(item => item.ThreadId == threadId && item.ReporterId == userId, context.RequestAborted))
        { db.CommunityReports.Add(CommunityReport.Create(userId.Value, threadId, request.Reason)); await db.SaveChangesAsync(context.RequestAborted); }
        return Results.NoContent();
    }
    private static async Task<IResult> ModerateThread(Guid threadId, ModerateCommunityThreadRequest request, HttpContext context, ProductDbContext db)
    {
        if (!context.User.IsInRole("Admin")) return Results.Forbid();
        var thread = await db.CommunityThreads.FindAsync([threadId], context.RequestAborted);
        if (thread is null) return Results.NotFound();
        thread.SetModeration(request.IsPinned, request.IsLocked); await db.SaveChangesAsync(context.RequestAborted); return Results.NoContent();
    }
}

public sealed record CreateCommunityThreadRequest(Guid? ProductId, string Title, string Body, CommunityThreadCategory Category, CommunityThreadKind Kind);
public sealed record CreateCommunityReplyRequest(string Body, Guid? ParentReplyId);
public sealed record ReportCommunityThreadRequest(string Reason);
public sealed record ModerateCommunityThreadRequest(bool IsPinned, bool IsLocked);
public record CommunityThreadSummaryResponse(Guid Id, Guid? ProductId, Guid AuthorId, string Title, string Slug, string Body, CommunityThreadCategory Category, CommunityThreadKind Kind, bool IsPinned, bool IsLocked, int ViewCount, int ReplyCount, int ReactionCount, int FollowerCount, bool IsReacted, bool IsFollowing, DateTime CreatedAtUtc, DateTime UpdatedAtUtc);
public sealed record CommunityReplyResponse(Guid Id, Guid AuthorId, Guid? ParentReplyId, string Body, bool IsOfficial, int ReactionCount, bool IsReacted, DateTime CreatedAtUtc, DateTime? EditedAtUtc);
public sealed record CommunityThreadDetailsResponse(Guid Id, Guid? ProductId, Guid AuthorId, string Title, string Slug, string Body, CommunityThreadCategory Category, CommunityThreadKind Kind, bool IsPinned, bool IsLocked, int ViewCount, int ReplyCount, int ReactionCount, int FollowerCount, bool IsReacted, bool IsFollowing, DateTime CreatedAtUtc, DateTime UpdatedAtUtc, IReadOnlyList<CommunityReplyResponse> Replies)
    : CommunityThreadSummaryResponse(Id, ProductId, AuthorId, Title, Slug, Body, Category, Kind, IsPinned, IsLocked, ViewCount, ReplyCount, ReactionCount, FollowerCount, IsReacted, IsFollowing, CreatedAtUtc, UpdatedAtUtc);
