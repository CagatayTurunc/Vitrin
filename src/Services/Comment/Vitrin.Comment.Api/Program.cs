using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Vitrin.Comment.Application.Commands;
using Vitrin.Comment.Infrastructure;
using Vitrin.Comment.Infrastructure.Data;
using Vitrin.Shared.Infrastructure.Auth;
using Vitrin.Shared.Infrastructure.Api;
using Vitrin.Shared.Infrastructure.Migrations;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHealthChecks();
builder.Services.AddVitrinJwtAuthentication(builder.Configuration);
builder.Services.AddVitrinApiErrors();

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

app.MapPost("/api/comments", async (HttpContext context, [FromBody] AddCommentRequest request, IMediator mediator) =>
{
    var userId = context.User.GetUserId();
    if (userId is null) return Results.Unauthorized();

    var command = new AddCommentCommand(
        request.ProductId,
        userId.Value,
        context.User.GetUsername(),
        request.Content,
        request.ParentCommentId);
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

app.MapGet("/api/comments/{productId}", async (Guid productId, CommentDbContext db) =>
{
    var comments = await db.Comments
        .AsNoTracking()
        .Where(c => c.ProductId == productId)
        .OrderByDescending(c => c.CreatedAt)
        .Take(500)
        .Select(comment => new
        {
            comment.Id,
            comment.ProductId,
            comment.UserId,
            comment.UserName,
            Content = comment.IsDeleted ? "[deleted]" : comment.Content,
            comment.CreatedAt,
            comment.ParentCommentId,
            comment.IsDeleted,
            comment.UpdatedAt
        })
        .ToListAsync();
    return Results.Ok(comments);
})
.WithName("GetCommentsByProduct")
.WithOpenApi();

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
public record AddCommentRequest(Guid ProductId, string Content, Guid? ParentCommentId = null);
