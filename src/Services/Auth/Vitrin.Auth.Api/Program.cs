using MediatR;
using Microsoft.EntityFrameworkCore;
using Vitrin.Auth.Application;
using Vitrin.Auth.Application.Commands;
using Vitrin.Auth.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHealthChecks();

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<Vitrin.Auth.Infrastructure.Data.AuthDbContext>();
    try {
        db.Database.Migrate();
    } catch {
        // Log error or ignore if DB is not ready, though depends_on should handle it
    }
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapHealthChecks("/health");

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

string? GetUserRoleFromRequest(HttpContext context)
{
    var authHeader = context.Request.Headers["Authorization"].FirstOrDefault();
    if (authHeader != null && authHeader.StartsWith("Bearer "))
    {
        var token = authHeader.Substring("Bearer ".Length).Trim();
        try {
            var parts = token.Split('.');
            if (parts.Length >= 2) {
                var payload = parts[1];
                payload = payload.Replace('-', '+').Replace('_', '/');
                while (payload.Length % 4 != 0) payload += "=";
                var json = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(payload));
                using var doc = System.Text.Json.JsonDocument.Parse(json);
                if (doc.RootElement.TryGetProperty("Role", out var roleEl) || doc.RootElement.TryGetProperty("role", out roleEl)) {
                    return roleEl.GetString();
                }
            }
        } catch { }
    }
    return null;
}

app.MapGet("/api/auth/admin/users", async (HttpContext context, Vitrin.Auth.Infrastructure.Data.AuthDbContext db) =>
{
    var role = GetUserRoleFromRequest(context);
    if (role != "Admin") return Results.Unauthorized();

    var users = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.ToListAsync(db.Users);
    return Results.Ok(users.Select(u => new { 
        u.Id, 
        u.Email, 
        u.Username, 
        u.FullName, 
        u.Headline,
        u.Role, 
        u.CreatedAt 
    }));
});

app.MapGet("/api/auth/users/by-username/{username}", async (string username, HttpContext context, Vitrin.Auth.Infrastructure.Data.AuthDbContext db) =>
{
    var user = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.FirstOrDefaultAsync(
        db.Users.Include(u => u.Badges), u => u.Username.ToLower() == username.ToLower());
        
    if (user == null) return Results.NotFound();
    
    var followerCount = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.CountAsync(db.UserFollows, uf => uf.FollowingId == user.Id);
    var followingCount = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.CountAsync(db.UserFollows, uf => uf.FollowerId == user.Id);
    
    bool isFollowing = false;
    var authHeader = context.Request.Headers["Authorization"].FirstOrDefault();
    if (authHeader != null && authHeader.StartsWith("Bearer ")) {
        try {
            var handler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
            var jwt = handler.ReadJwtToken(authHeader.Substring("Bearer ".Length));
            var sub = jwt.Claims.FirstOrDefault(c => c.Type == "sub")?.Value;
            if (Guid.TryParse(sub, out var currentUserId)) {
                isFollowing = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.AnyAsync(db.UserFollows, uf => uf.FollowerId == currentUserId && uf.FollowingId == user.Id);
            }
        } catch {}
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
    var user = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.FirstOrDefaultAsync(db.Users.Include(u => u.Badges), u => u.Id == userId);
    if (user == null) return Results.NotFound();
    
    var followerCount = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.CountAsync(db.UserFollows, uf => uf.FollowingId == user.Id);
    var followingCount = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.CountAsync(db.UserFollows, uf => uf.FollowerId == user.Id);
    
    bool isFollowing = false;
    var authHeader = context.Request.Headers["Authorization"].FirstOrDefault();
    if (authHeader != null && authHeader.StartsWith("Bearer ")) {
        try {
            var handler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
            var jwt = handler.ReadJwtToken(authHeader.Substring("Bearer ".Length));
            var sub = jwt.Claims.FirstOrDefault(c => c.Type == "sub")?.Value;
            if (Guid.TryParse(sub, out var currentUserId)) {
                isFollowing = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.AnyAsync(db.UserFollows, uf => uf.FollowerId == currentUserId && uf.FollowingId == user.Id);
            }
        } catch {}
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
    var authHeader = context.Request.Headers["Authorization"].FirstOrDefault();
    if (authHeader == null || !authHeader.StartsWith("Bearer ")) return Results.Unauthorized();
    var token = authHeader.Substring("Bearer ".Length);
    Guid userId;
    try {
        var handler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(token);
        var sub = jwt.Claims.FirstOrDefault(c => c.Type == "sub")?.Value;
        if (!Guid.TryParse(sub, out userId)) return Results.Unauthorized();
    } catch { return Results.Unauthorized(); }
    
    var user = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.FirstOrDefaultAsync(db.Users.Include(u => u.Badges), u => u.Id == userId);
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
    });
});

app.MapPut("/api/auth/users/me", async (HttpContext context, [Microsoft.AspNetCore.Mvc.FromBody] UpdateProfileRequest request, Vitrin.Auth.Infrastructure.Data.AuthDbContext db) =>
{
    var authHeader = context.Request.Headers["Authorization"].FirstOrDefault();
    if (authHeader == null || !authHeader.StartsWith("Bearer ")) return Results.Unauthorized();
    var token = authHeader.Substring("Bearer ".Length);
    Guid userId;
    try {
        var handler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(token);
        var sub = jwt.Claims.FirstOrDefault(c => c.Type == "sub")?.Value;
        if (!Guid.TryParse(sub, out userId)) return Results.Unauthorized();
    } catch { return Results.Unauthorized(); }
    
    var user = await db.Users.FindAsync(userId);
    if (user == null) return Results.NotFound();
    
    // Check if new username is taken by someone else
    if (request.Username != user.Username && await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.AnyAsync(db.Users, u => u.Username == request.Username && u.Id != userId))
    {
        return Results.BadRequest("This username is already taken.");
    }
    
    user.UpdateProfile(request.FullName, request.Username, request.Headline, request.About, request.AvatarUrl, request.WebsiteUrl, request.GithubUrl, request.LinkedInUrl);
    await db.SaveChangesAsync();
    
    return Results.Ok(new { Message = "Profile updated successfully." });
});

app.MapPost("/api/auth/admin/users/{id}/role", async (Guid id, HttpContext context, [Microsoft.AspNetCore.Mvc.FromBody] int role, Vitrin.Auth.Infrastructure.Data.AuthDbContext db) =>
{
    var currentUserRole = GetUserRoleFromRequest(context);
    if (currentUserRole != "Admin") return Results.Unauthorized();

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

app.MapGet("/api/auth/admin/maker-applications", async (HttpContext context, Vitrin.Auth.Infrastructure.Data.AuthDbContext db) =>
{
    var role = GetUserRoleFromRequest(context);
    if (role != "Admin") return Results.Unauthorized();

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

app.MapPost("/api/auth/admin/maker-applications/{id}/approve", async (Guid id, HttpContext context, Vitrin.Auth.Infrastructure.Data.AuthDbContext db) =>
{
    var role = GetUserRoleFromRequest(context);
    if (role != "Admin") return Results.Unauthorized();

    var appToApprove = await db.MakerApplications.FindAsync(id);
    if (appToApprove == null) return Results.NotFound();
    
    appToApprove.Approve();
    
    // Also change user role to Maker (1)
    var user = await db.Users.FindAsync(appToApprove.UserId);
    if (user != null) {
        user.UpdateRole(Vitrin.Auth.Domain.Entities.UserRole.Maker);
        
        try {
            using var client = new HttpClient();
            var notifPayload = new { userId = user.Id, message = "Tebrikler, artık Maker oldunuz!" };
            var content = new StringContent(System.Text.Json.JsonSerializer.Serialize(notifPayload), System.Text.Encoding.UTF8, "application/json");
            await client.PostAsync("http://vitrin-notification:8080/api/notifications", content);
        } catch { }
    }
    
    await db.SaveChangesAsync();
    return Results.Ok(new { Message = "Approved successfully." });
});

app.MapPost("/api/auth/admin/maker-applications/{id}/reject", async (Guid id, HttpContext context, Vitrin.Auth.Infrastructure.Data.AuthDbContext db) =>
{
    var role = GetUserRoleFromRequest(context);
    if (role != "Admin") return Results.Unauthorized();

    var appToReject = await db.MakerApplications.FindAsync(id);
    if (appToReject == null) return Results.NotFound();
    
    appToReject.Reject();
    await db.SaveChangesAsync();
    return Results.Ok(new { Message = "Rejected successfully." });
});

// FOLLOW SYSTEM
app.MapPost("/api/auth/users/{username}/follow", async (string username, HttpContext context, Vitrin.Auth.Infrastructure.Data.AuthDbContext db, Microsoft.Extensions.Configuration.IConfiguration configuration) =>
{
    var authHeader = context.Request.Headers["Authorization"].FirstOrDefault();
    if (authHeader == null || !authHeader.StartsWith("Bearer ")) return Results.Unauthorized();
    var token = authHeader.Substring("Bearer ".Length);
    Guid followerId;
    try {
        var handler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(token);
        var sub = jwt.Claims.FirstOrDefault(c => c.Type == "sub")?.Value;
        if (!Guid.TryParse(sub, out followerId)) return Results.Unauthorized();
    } catch { return Results.Unauthorized(); }

    var userToFollow = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.FirstOrDefaultAsync(db.Users, u => u.Username.ToLower() == username.ToLower());
    if (userToFollow == null) return Results.NotFound();
    if (userToFollow.Id == followerId) return Results.BadRequest("You cannot follow yourself.");

    var existingFollow = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.FirstOrDefaultAsync(db.UserFollows, uf => uf.FollowerId == followerId && uf.FollowingId == userToFollow.Id);
    if (existingFollow != null) return Results.Ok(new { Message = "Already following." });

    db.UserFollows.Add(new Vitrin.Auth.Domain.Entities.UserFollow(followerId, userToFollow.Id));
    await db.SaveChangesAsync();

    // Send notification
    try {
        var notificationUrl = configuration["ServiceUrls:Notification"] ?? "http://vitrin-notification:8080";
        var followerUser = await db.Users.FindAsync(followerId);
        var notifPayload = new { userId = userToFollow.Id, message = $"@{followerUser?.Username} sizi takip etmeye başladı." };
        var content = new System.Net.Http.StringContent(System.Text.Json.JsonSerializer.Serialize(notifPayload), System.Text.Encoding.UTF8, "application/json");
        using var client = new System.Net.Http.HttpClient();
        await client.PostAsync($"{notificationUrl}/api/notifications", content);
    } catch { }

    return Results.Ok(new { Message = "Followed successfully." });
});

app.MapDelete("/api/auth/users/{username}/follow", async (string username, HttpContext context, Vitrin.Auth.Infrastructure.Data.AuthDbContext db) =>
{
    var authHeader = context.Request.Headers["Authorization"].FirstOrDefault();
    if (authHeader == null || !authHeader.StartsWith("Bearer ")) return Results.Unauthorized();
    var token = authHeader.Substring("Bearer ".Length);
    Guid followerId;
    try {
        var handler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(token);
        var sub = jwt.Claims.FirstOrDefault(c => c.Type == "sub")?.Value;
        if (!Guid.TryParse(sub, out followerId)) return Results.Unauthorized();
    } catch { return Results.Unauthorized(); }

    var userToUnfollow = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.FirstOrDefaultAsync(db.Users, u => u.Username.ToLower() == username.ToLower());
    if (userToUnfollow == null) return Results.NotFound();

    var existingFollow = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.FirstOrDefaultAsync(db.UserFollows, uf => uf.FollowerId == followerId && uf.FollowingId == userToUnfollow.Id);
    if (existingFollow == null) return Results.Ok(new { Message = "Not following." });

    db.UserFollows.Remove(existingFollow);
    await db.SaveChangesAsync();

    return Results.Ok(new { Message = "Unfollowed successfully." });
});

app.MapGet("/api/auth/users/{username}/followers", async (string username, Vitrin.Auth.Infrastructure.Data.AuthDbContext db) =>
{
    var user = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.FirstOrDefaultAsync(db.Users, u => u.Username.ToLower() == username.ToLower());
    if (user == null) return Results.NotFound();

    var followers = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.ToListAsync(
        db.UserFollows
          .Where(uf => uf.FollowingId == user.Id)
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
    var user = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.FirstOrDefaultAsync(db.Users, u => u.Username.ToLower() == username.ToLower());
    if (user == null) return Results.NotFound();

    var following = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.ToListAsync(
        db.UserFollows
          .Where(uf => uf.FollowerId == user.Id)
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
          .Where(uf => uf.FollowingId == userId)
          .Select(uf => uf.FollowerId)
    );

    return Results.Ok(followerIds);
});

// Gamification
app.MapPost("/api/auth/users/me/record-vote", async (HttpContext context, Vitrin.Auth.Infrastructure.Data.AuthDbContext db) =>
{
    var authHeader = context.Request.Headers["Authorization"].FirstOrDefault();
    if (authHeader == null || !authHeader.StartsWith("Bearer ")) return Results.Unauthorized();
    var token = authHeader.Substring("Bearer ".Length);
    Guid userId;
    try {
        var handler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(token);
        var sub = jwt.Claims.FirstOrDefault(c => c.Type == "sub")?.Value;
        if (!Guid.TryParse(sub, out userId)) return Results.Unauthorized();
    } catch { return Results.Unauthorized(); }

    var user = await db.Users.FindAsync(userId);
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
});

// Gamification - Leaderboard
app.MapGet("/api/auth/leaderboard", async (Vitrin.Auth.Infrastructure.Data.AuthDbContext db) =>
{
    var topStreaks = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.ToListAsync(
        db.Users
          .OrderByDescending(u => u.CurrentStreak)
          .Take(10)
          .Select(u => new { u.Id, u.Username, u.FullName, u.AvatarUrl, u.Headline, u.CurrentStreak })
    );

    // SQL'de Follower sayısını hesaplayarak sıralamak için Follower sistemini kullanıyoruz.
    var topMakers = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.ToListAsync(
        db.Users
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

app.Run();

public record MakerApplicationRequest(Guid UserId, string PortfolioUrl, string Reason);
public record UpdateProfileRequest(string FullName, string Username, string? Headline, string? About, string? AvatarUrl, string? WebsiteUrl, string? GithubUrl, string? LinkedInUrl);
