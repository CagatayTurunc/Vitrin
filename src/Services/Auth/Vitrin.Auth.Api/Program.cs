using MediatR;
using Vitrin.Auth.Application;
using Vitrin.Auth.Application.Commands;
using Vitrin.Auth.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapPost("/api/auth/register", async (RegisterCommand command, IMediator mediator) =>
{
    var result = await mediator.Send(command);
    return result.IsSuccess ? Results.Ok(result.Value) : Results.BadRequest(result.Error);
});

app.MapPost("/api/auth/login", async (LoginCommand command, IMediator mediator) =>
{
    var result = await mediator.Send(command);
    return result.IsSuccess ? Results.Ok(result.Value) : Results.BadRequest(result.Error);
});

app.MapPost("/api/auth/external-login", async (ExternalLoginCommand command, IMediator mediator) =>
{
    var result = await mediator.Send(command);
    return result.IsSuccess ? Results.Ok(result.Value) : Results.BadRequest(result.Error);
});

app.MapGet("/api/auth/admin/users", async (Vitrin.Auth.Infrastructure.Data.AuthDbContext db) =>
{
    var users = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.ToListAsync(db.Users);
    return Results.Ok(users.Select(u => new { 
        u.Id, 
        u.Email, 
        u.Username, 
        u.FullName, 
        u.Role, 
        u.CreatedAt 
    }));
});

app.MapPost("/api/auth/admin/users/{id}/role", async (Guid id, [Microsoft.AspNetCore.Mvc.FromBody] int role, Vitrin.Auth.Infrastructure.Data.AuthDbContext db) =>
{
    var user = await db.Users.FindAsync(id);
    if (user == null) return Results.NotFound("User not found");
    
    user.UpdateRole((Vitrin.Auth.Domain.Entities.UserRole)role);
    await db.SaveChangesAsync();
    
    return Results.Ok(new { Message = "User role updated successfully" });
});

// MAKER APPLICATIONS
app.MapPost("/api/auth/maker-applications", async ([Microsoft.AspNetCore.Mvc.FromBody] MakerApplicationRequest request, Vitrin.Auth.Infrastructure.Data.AuthDbContext db) =>
{
    var application = Vitrin.Auth.Domain.Entities.MakerApplication.Create(request.UserId, request.PortfolioUrl, request.Reason);
    db.MakerApplications.Add(application);
    await db.SaveChangesAsync();
    return Results.Ok(new { Message = "Application submitted successfully." });
});

app.MapGet("/api/auth/admin/maker-applications", async (Vitrin.Auth.Infrastructure.Data.AuthDbContext db) =>
{
    var apps = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.ToListAsync(
        db.MakerApplications.Where(m => m.Status == Vitrin.Auth.Domain.Entities.ApplicationStatus.Pending)
    );
    
    // We want to return user details as well
    var users = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.ToListAsync(db.Users);
    
    var result = apps.Select(a => new {
        a.Id,
        a.PortfolioUrl,
        a.Reason,
        a.CreatedAt,
        User = users.FirstOrDefault(u => u.Id == a.UserId)?.Email,
        FullName = users.FirstOrDefault(u => u.Id == a.UserId)?.FullName
    });
    
    return Results.Ok(result);
});

app.MapPost("/api/auth/admin/maker-applications/{id}/approve", async (Guid id, Vitrin.Auth.Infrastructure.Data.AuthDbContext db) =>
{
    var appToApprove = await db.MakerApplications.FindAsync(id);
    if (appToApprove == null) return Results.NotFound();
    
    appToApprove.Approve();
    
    // Also change user role to Maker (1)
    var user = await db.Users.FindAsync(appToApprove.UserId);
    if (user != null) {
        user.UpdateRole(Vitrin.Auth.Domain.Entities.UserRole.Maker);
    }
    
    await db.SaveChangesAsync();
    return Results.Ok(new { Message = "Approved successfully." });
});

app.MapPost("/api/auth/admin/maker-applications/{id}/reject", async (Guid id, Vitrin.Auth.Infrastructure.Data.AuthDbContext db) =>
{
    var appToReject = await db.MakerApplications.FindAsync(id);
    if (appToReject == null) return Results.NotFound();
    
    appToReject.Reject();
    await db.SaveChangesAsync();
    return Results.Ok(new { Message = "Rejected successfully." });
});

app.Run();

public record MakerApplicationRequest(Guid UserId, string PortfolioUrl, string Reason);
