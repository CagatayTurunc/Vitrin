using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Vitrin.Voting.Application.Commands;
using Vitrin.Voting.Infrastructure;
using Vitrin.Voting.Infrastructure.Data;
using Vitrin.Shared.Infrastructure.Auth;
using Vitrin.Shared.Infrastructure.Api;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHealthChecks();
builder.Services.AddVitrinJwtAuthentication(builder.Configuration);
builder.Services.AddVitrinApiErrors();

// MediatR
builder.Services.AddMediatR(cfg =>
    cfg.RegisterServicesFromAssembly(typeof(AddVoteCommand).Assembly));

// Infrastructure: DbContext + Repository + Kafka Publisher
builder.Services.AddVotingInfrastructure(builder.Configuration);

var app = builder.Build();

app.UseVitrinApiErrors();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<VoteDbContext>();
    db.Database.Migrate();
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthentication();
app.UseAuthorization();

app.MapHealthChecks("/health");

// Oy ekle
app.MapPost("/api/votes", async (HttpContext context, [FromBody] VoteRequest request, IMediator mediator) =>
{
    var userId = context.User.GetUserId();
    if (userId is null) return Results.Unauthorized();

    var command = new AddVoteCommand(userId.Value, request.ProductId);
    var result = await mediator.Send(command);
    return result.IsSuccess
        ? Results.Ok(new { Message = "Vote added successfully!" })
        : ApiProblemResults.BadRequest(result.Error, "vote.add_failed");
})
.WithName("AddVote")
.WithOpenApi()
.RequireAuthorization();

// Oy geri al
app.MapDelete("/api/votes", async (HttpContext context, [FromBody] VoteRequest request, IMediator mediator) =>
{
    var userId = context.User.GetUserId();
    if (userId is null) return Results.Unauthorized();

    var command = new RemoveVoteCommand(userId.Value, request.ProductId);
    var result = await mediator.Send(command);
    return result.IsSuccess
        ? Results.Ok(new { Message = "Vote removed successfully!" })
        : ApiProblemResults.BadRequest(result.Error, "vote.remove_failed");
})
.WithName("RemoveVote")
.WithOpenApi()
.RequireAuthorization();

app.MapGet("/api/votes/me", async (HttpContext context, VoteDbContext db) =>
{
    var userId = context.User.GetUserId();
    if (userId is null) return Results.Unauthorized();

    var productIds = await db.Votes
        .AsNoTracking()
        .Where(vote => vote.UserId == userId.Value)
        .Select(vote => vote.ProductId)
        .ToListAsync(context.RequestAborted);

    return Results.Ok(productIds);
})
.WithName("GetMyVotes")
.WithOpenApi()
.RequireAuthorization();

// Tüm oyları listele (debug / admin)
app.MapGet("/api/votes", async (VoteDbContext db) =>
{
    var votes = await db.Votes.ToListAsync();
    return Results.Ok(votes);
})
.WithName("GetVotes")
.WithOpenApi()
.RequireAuthorization(VitrinAuthDefaults.AdminPolicy);

// Belirli ürünün oy sayısı
app.MapGet("/api/votes/count/{productId:guid}", async (Guid productId, VoteDbContext db) =>
{
    var count = await db.Votes.CountAsync(v => v.ProductId == productId);
    return Results.Ok(new { ProductId = productId, Count = count });
})
.WithName("GetVoteCount")
.WithOpenApi();

app.Run();

public record VoteRequest(Guid ProductId);
