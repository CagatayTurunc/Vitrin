using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Vitrin.Comment.Application.Commands;
using Vitrin.Comment.Infrastructure;
using Vitrin.Comment.Infrastructure.Data;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHealthChecks();

// MediatR
builder.Services.AddMediatR(cfg =>
    cfg.RegisterServicesFromAssembly(typeof(AddCommentCommand).Assembly));

// Infrastructure: DbContext + Repository + Kafka Publisher
builder.Services.AddCommentInfrastructure(builder.Configuration);

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<CommentDbContext>();
    db.Database.Migrate();
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapHealthChecks("/health");

app.MapPost("/api/comments", async ([FromBody] AddCommentCommand command, IMediator mediator) =>
{
    var result = await mediator.Send(command);
    if (result.IsSuccess)
    {
        return Results.Ok(new { CommentId = result.Value, Message = "Comment added successfully!" });
    }
    return Results.BadRequest(new { Error = result.Error });
})
.WithName("AddComment")
.WithOpenApi();

app.MapGet("/api/comments/{productId}", async (Guid productId, CommentDbContext db) =>
{
    var comments = await db.Comments
        .Where(c => c.ProductId == productId)
        .OrderByDescending(c => c.CreatedAt)
        .ToListAsync();
    return Results.Ok(comments);
})
.WithName("GetCommentsByProduct")
.WithOpenApi();

Guid? GetUserIdFromRequest(HttpContext context)
{
    var authHeader = context.Request.Headers["Authorization"].FirstOrDefault();
    if (authHeader != null && authHeader.StartsWith("Bearer "))
    {
        var token = authHeader.Substring("Bearer ".Length);
        try {
            var handler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
            var jwt = handler.ReadJwtToken(token);
            var sub = jwt.Claims.FirstOrDefault(c => c.Type == System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub)?.Value;
            if (Guid.TryParse(sub, out Guid userId)) return userId;
        } catch { }
    }
    return null;
}

app.MapPut("/api/comments/{id}", async (Guid id, [FromBody] UpdateCommentRequest req, HttpContext context, IMediator mediator) =>
{
    var userId = GetUserIdFromRequest(context);
    if (userId == null) return Results.Unauthorized();

    var result = await mediator.Send(new UpdateCommentCommand(id, userId.Value, req.Content));
    if (result.IsSuccess)
    {
        return Results.Ok(new { Message = "Comment updated successfully!" });
    }
    return Results.BadRequest(new { Error = result.Error });
});

app.MapDelete("/api/comments/{id}", async (Guid id, HttpContext context, IMediator mediator) =>
{
    var userId = GetUserIdFromRequest(context);
    if (userId == null) return Results.Unauthorized();

    var result = await mediator.Send(new DeleteCommentCommand(id, userId.Value));
    if (result.IsSuccess)
    {
        return Results.Ok(new { Message = "Comment deleted successfully!" });
    }
    return Results.BadRequest(new { Error = result.Error });
});

app.Run();

public record UpdateCommentRequest(string Content);
