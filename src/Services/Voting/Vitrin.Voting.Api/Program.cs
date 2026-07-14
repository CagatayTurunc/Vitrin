using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Vitrin.Voting.Application.Commands;
using Vitrin.Voting.Infrastructure;
using Vitrin.Voting.Infrastructure.Data;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHealthChecks();

// MediatR
builder.Services.AddMediatR(cfg =>
    cfg.RegisterServicesFromAssembly(typeof(AddVoteCommand).Assembly));

// Infrastructure: DbContext + Repository + Kafka Publisher
builder.Services.AddVotingInfrastructure(builder.Configuration);

var app = builder.Build();

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

app.MapHealthChecks("/health");

// Oy ekle
app.MapPost("/api/votes", async ([FromBody] AddVoteCommand command, IMediator mediator) =>
{
    var result = await mediator.Send(command);
    return result.IsSuccess
        ? Results.Ok(new { Message = "Vote added successfully!" })
        : Results.BadRequest(new { Error = result.Error });
})
.WithName("AddVote")
.WithOpenApi();

// Oy geri al
app.MapDelete("/api/votes", async ([FromBody] RemoveVoteCommand command, IMediator mediator) =>
{
    var result = await mediator.Send(command);
    return result.IsSuccess
        ? Results.Ok(new { Message = "Vote removed successfully!" })
        : Results.BadRequest(new { Error = result.Error });
})
.WithName("RemoveVote")
.WithOpenApi();

// Tüm oyları listele (debug / admin)
app.MapGet("/api/votes", async (VoteDbContext db) =>
{
    var votes = await db.Votes.ToListAsync();
    return Results.Ok(votes);
})
.WithName("GetVotes")
.WithOpenApi();

// Belirli ürünün oy sayısı
app.MapGet("/api/votes/count/{productId:guid}", async (Guid productId, VoteDbContext db) =>
{
    var count = await db.Votes.CountAsync(v => v.ProductId == productId);
    return Results.Ok(new { ProductId = productId, Count = count });
})
.WithName("GetVoteCount")
.WithOpenApi();

app.Run();
