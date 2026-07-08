using Microsoft.EntityFrameworkCore;
using Vitrin.Voting.Application.Commands;
using Vitrin.Voting.Infrastructure.Data;
using Vitrin.Voting.Infrastructure.Repositories;
using MediatR;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Register MediatR
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(AddVoteCommand).Assembly));

// EF Core SQLite
builder.Services.AddDbContext<VoteDbContext>(options =>
    options.UseSqlite("Data Source=voting_db.sqlite"));

// Register Real Repository
builder.Services.AddScoped<IVoteRepository, VoteRepository>();

var app = builder.Build();

// Migrate Database on startup
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

app.MapPost("/api/votes", async ([FromBody] AddVoteCommand command, IMediator mediator) =>
{
    var result = await mediator.Send(command);
    if (result.IsSuccess)
    {
        return Results.Ok(new { Message = "Vote added successfully!" });
    }
    return Results.BadRequest(new { Error = result.Error });
})
.WithName("AddVote")
.WithOpenApi();

app.MapGet("/api/votes", async (VoteDbContext db) =>
{
    var votes = await db.Votes.ToListAsync();
    return Results.Ok(votes);
})
.WithName("GetVotes")
.WithOpenApi();

app.Run();
