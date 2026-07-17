using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Vitrin.Comment.Application.Commands;
using Vitrin.Comment.Domain.Entities;
using Vitrin.Comment.Infrastructure;
using Vitrin.Comment.Infrastructure.Data;
using Vitrin.Shared.Infrastructure.Auth;
using Vitrin.Shared.Infrastructure.Api;
using Vitrin.Shared.Infrastructure.Audit;
using Vitrin.Shared.Infrastructure.Migrations;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHealthChecks();
builder.Services.AddVitrinJwtAuthentication(builder.Configuration);
builder.Services.AddVitrinApiErrors();
builder.Services.AddVitrinAuditLogging();

// MediatR
builder.Services.AddMediatR(cfg =>
    cfg.RegisterServicesFromAssembly(typeof(AddCommentCommand).Assembly));

// Infrastructure: DbContext + Repository + Kafka Publisher
builder.Services.AddCommentInfrastructure(builder.Configuration);

var app = builder.Build();

app.UseVitrinApiErrors();

if (await app.MigrateDatabaseAndExitAsync<CommentDbContext>(
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

app.MapPost("/api/comments", async (
    HttpContext context,
    [FromBody] AddCommentRequest request,
    IMediator mediator,
    ICommentMentionResolver mentionResolver) =>
{
    var userId = context.User.GetUserId();
    if (userId is null) return Results.Unauthorized();

    var mentions = await mentionResolver.ResolveAsync(request.Content, context.RequestAborted);
    var command = new AddCommentCommand(
        request.ProductId,
        userId.Value,
        context.User.GetUsername(),
        request.Content,
        request.ParentCommentId,
        request.ProductMakerId,
        mentions);
    var result = await mediator.Send(command);
    if (result.IsSuccess)
    {
        return Results.Ok(new { CommentId = result.Value, Message = "Comment added successfully!" });
    }
    return ApiProblemResults.BadRequest(result.Error, "comment.create_failed");
})
.WithName("AddComment")
.WithOpenApi()
.RequireAuthorization();

app.MapGet("/api/comments/{productId}", async (Guid productId, HttpContext context, CommentDbContext db) =>
{
    var currentUserId = context.User.GetUserId();
    var comments = await db.Comments
        .AsNoTracking()
        .Include(comment => comment.Mentions)
        .Include(comment => comment.Reactions)
        .Where(c => c.ProductId == productId)
        .OrderByDescending(c => c.CreatedAt)
        .Take(500)
        .ToListAsync();

    var response = comments.Select(comment => new
        {
            comment.Id,
            comment.ProductId,
            comment.UserId,
            comment.UserName,
            Content = comment.IsDeleted
                ? "[deleted]"
                : comment.ModerationStatus == CommentModerationStatus.Hidden
                    ? "[moderated]"
                    : comment.Content,
            comment.CreatedAt,
            comment.ParentCommentId,
            comment.IsDeleted,
            comment.UpdatedAt,
            comment.ModerationStatus,
            IsModerated = comment.ModerationStatus == CommentModerationStatus.Hidden,
            MentionedUsers = comment.Mentions.Select(mention => new
            {
                mention.MentionedUserId,
                mention.MentionedUsername
            }),
            ReactionCounts = comment.Reactions
                .GroupBy(reaction => reaction.ReactionType.ToString().ToLowerInvariant())
                .ToDictionary(group => group.Key, group => group.Count()),
            MyReaction = currentUserId.HasValue
                ? comment.Reactions
                    .Where(reaction => reaction.UserId == currentUserId.Value)
                    .Select(reaction => reaction.ReactionType.ToString().ToLowerInvariant())
                    .FirstOrDefault()
                : null
        })
        .ToList();
    return Results.Ok(response);
})
.WithName("GetCommentsByProduct")
.WithOpenApi();

app.MapPut("/api/comments/{id:guid}/reactions", async (
    Guid id,
    [FromBody] SetCommentReactionRequest request,
    HttpContext context,
    CommentDbContext db,
    ICommentNotificationPublisher notificationPublisher) =>
{
    var userId = context.User.GetUserId();
    if (userId is null) return Results.Unauthorized();
    if (!Enum.TryParse<CommentReactionType>(request.Reaction, true, out var reactionType))
        return ApiProblemResults.BadRequest("Unknown reaction type.", "comment.reaction_invalid");

    var comment = await db.Comments
        .Include(item => item.Reactions)
        .FirstOrDefaultAsync(item => item.Id == id);
    if (comment is null) return ApiProblemResults.NotFound("Comment not found.", "comment.not_found");
    if (comment.IsDeleted || comment.ModerationStatus == CommentModerationStatus.Hidden)
        return ApiProblemResults.BadRequest("This comment cannot receive reactions.", "comment.reaction_unavailable");

    var isNew = comment.SetReaction(userId.Value, context.User.GetUsername(), reactionType);
    if (isNew && comment.UserId != userId.Value)
    {
        await notificationPublisher.NotifyAsync(
            comment.UserId,
            $"@{context.User.GetUsername()} yorumunuza {reactionType.ToString().ToLowerInvariant()} tepkisi verdi.",
            "comment_reaction",
            context.RequestAborted);
    }

    await db.SaveChangesAsync(context.RequestAborted);
    var counts = comment.Reactions
        .GroupBy(reaction => reaction.ReactionType.ToString().ToLowerInvariant())
        .ToDictionary(group => group.Key, group => group.Count());
    return Results.Ok(new { ReactionCounts = counts, MyReaction = reactionType.ToString().ToLowerInvariant() });
}).RequireAuthorization();

app.MapDelete("/api/comments/{id:guid}/reactions", async (
    Guid id,
    HttpContext context,
    CommentDbContext db) =>
{
    var userId = context.User.GetUserId();
    if (userId is null) return Results.Unauthorized();

    var comment = await db.Comments
        .Include(item => item.Reactions)
        .FirstOrDefaultAsync(item => item.Id == id);
    if (comment is null) return ApiProblemResults.NotFound("Comment not found.", "comment.not_found");

    comment.RemoveReaction(userId.Value);
    await db.SaveChangesAsync(context.RequestAborted);
    var counts = comment.Reactions
        .GroupBy(reaction => reaction.ReactionType.ToString().ToLowerInvariant())
        .ToDictionary(group => group.Key, group => group.Count());
    return Results.Ok(new { ReactionCounts = counts, MyReaction = (string?)null });
}).RequireAuthorization();

app.MapGet("/api/comments/activity", async (int? limit, CommentDbContext db) =>
{
    var take = Math.Clamp(limit ?? 30, 1, 100);
    var comments = await db.Comments
        .AsNoTracking()
        .Where(comment => !comment.IsDeleted && comment.ModerationStatus == CommentModerationStatus.Visible)
        .OrderByDescending(comment => comment.CreatedAt)
        .Take(take)
        .Select(comment => new ActivityFeedResponse(
            $"comment:{comment.Id}",
            "comment",
            comment.UserId,
            comment.UserName,
            comment.ParentCommentId.HasValue ? "bir yoruma cevap verdi" : "bir ürüne yorum yaptı",
            "Comment",
            comment.Id,
            comment.ProductId,
            comment.Content,
            comment.CreatedAt))
        .ToListAsync();

    var reactions = await (
        from reaction in db.CommentReactions.AsNoTracking()
        join comment in db.Comments.AsNoTracking() on reaction.CommentId equals comment.Id
        where !comment.IsDeleted && comment.ModerationStatus == CommentModerationStatus.Visible
        orderby (reaction.UpdatedAtUtc ?? reaction.CreatedAtUtc) descending
        select new ActivityFeedResponse(
            $"reaction:{reaction.Id}",
            "reaction",
            reaction.UserId,
            reaction.UserName,
            "bir yoruma tepki verdi",
            "Comment",
            comment.Id,
            comment.ProductId,
            reaction.ReactionType.ToString().ToLower(),
            reaction.UpdatedAtUtc ?? reaction.CreatedAtUtc))
        .Take(take)
        .ToListAsync();

    return Results.Ok(comments.Concat(reactions)
        .OrderByDescending(item => item.CreatedAtUtc)
        .Take(take));
});

app.MapPatch("/api/comments/admin/{id:guid}/moderation", async (
    Guid id,
    [FromBody] ModerateCommentRequest request,
    HttpContext context,
    CommentDbContext db,
    IAuditLogger auditLogger) =>
{
    var moderatorUserId = context.User.GetUserId();
    if (moderatorUserId is null) return Results.Unauthorized();
    if (!Enum.TryParse<CommentModerationStatus>(request.Status, true, out var status))
        return ApiProblemResults.BadRequest("Unknown moderation status.", "comment.moderation_status_invalid");
    if (status == CommentModerationStatus.Hidden && string.IsNullOrWhiteSpace(request.Reason))
        return ApiProblemResults.BadRequest("A reason is required when hiding a comment.", "comment.moderation_reason_required");

    var comment = await db.Comments.FindAsync([id], context.RequestAborted);
    if (comment is null) return ApiProblemResults.NotFound("Comment not found.", "comment.not_found");

    comment.Moderate(status, moderatorUserId.Value, request.Reason ?? string.Empty);
    db.CommentModerationActions.Add(CommentModerationAction.Create(
        id,
        moderatorUserId.Value,
        status,
        request.Reason ?? "Restored by moderator"));
    await db.SaveChangesAsync(context.RequestAborted);
    await auditLogger.WriteAsync(
        new AuditEvent(
            status == CommentModerationStatus.Hidden ? "admin.comment_hidden" : "admin.comment_restored",
            moderatorUserId,
            "Comment",
            id.ToString(),
            "Succeeded",
            context.TraceIdentifier),
        context.RequestAborted);

    return Results.Ok(new { comment.Id, comment.ModerationStatus, comment.ModerationReason });
}).RequireAuthorization(VitrinAuthDefaults.AdminPolicy);

app.MapGet("/api/comments/admin/audit", async (CommentDbContext db) =>
{
    var actions = await db.CommentModerationActions
        .AsNoTracking()
        .OrderByDescending(action => action.CreatedAtUtc)
        .Take(200)
        .Select(action => new
        {
            action.Id,
            Action = action.Status == CommentModerationStatus.Hidden ? "admin.comment_hidden" : "admin.comment_restored",
            ActorUserId = action.ModeratorUserId,
            ResourceType = "Comment",
            ResourceId = action.CommentId.ToString(),
            Outcome = "Succeeded",
            Details = action.Reason,
            OccurredAtUtc = action.CreatedAtUtc
        })
        .ToListAsync();
    return Results.Ok(actions);
}).RequireAuthorization(VitrinAuthDefaults.AdminPolicy);

app.MapPut("/api/comments/{id}", async (Guid id, [FromBody] UpdateCommentRequest req, HttpContext context, IMediator mediator) =>
{
    var userId = context.User.GetUserId();
    if (userId == null) return Results.Unauthorized();

    var result = await mediator.Send(new UpdateCommentCommand(id, userId.Value, req.Content));
    if (result.IsSuccess)
    {
        return Results.Ok(new { Message = "Comment updated successfully!" });
    }
    return ApiProblemResults.BadRequest(result.Error, "comment.update_failed");
}).RequireAuthorization();

app.MapDelete("/api/comments/{id}", async (Guid id, HttpContext context, IMediator mediator) =>
{
    var userId = context.User.GetUserId();
    if (userId == null) return Results.Unauthorized();

    var result = await mediator.Send(new DeleteCommentCommand(id, userId.Value));
    if (result.IsSuccess)
    {
        return Results.Ok(new { Message = "Comment deleted successfully!" });
    }
    return ApiProblemResults.BadRequest(result.Error, "comment.delete_failed");
}).RequireAuthorization();

app.Run();

public record UpdateCommentRequest(string Content);
public record AddCommentRequest(
    Guid ProductId,
    string Content,
    Guid? ParentCommentId = null,
    Guid? ProductMakerId = null);
public record SetCommentReactionRequest(string Reaction);
public record ModerateCommentRequest(string Status, string? Reason);
public record ActivityFeedResponse(
    string Id,
    string Type,
    Guid ActorUserId,
    string ActorUsername,
    string Summary,
    string EntityType,
    Guid EntityId,
    Guid? ProductId,
    string? Metadata,
    DateTime CreatedAtUtc);
