using System.Net;
using System.Text.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Vitrin.Notification.Domain.Entities;
using Vitrin.Notification.Infrastructure.Data;

namespace Vitrin.IntegrationTests.Api;

[Trait("Category", "Integration")]
public sealed class NotificationAuthorizationTests(NotificationApiFactory factory)
    : IClassFixture<NotificationApiFactory>
{
    [Fact]
    public async Task My_notifications_rejects_anonymous_requests()
    {
        await factory.ResetDatabaseAsync();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/notifications/me");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task My_notifications_only_returns_the_authenticated_users_rows()
    {
        await factory.ResetDatabaseAsync();
        var userId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();
        await SeedNotificationsAsync(
            NotificationItem.Create(userId, "Visible notification").Value,
            NotificationItem.Create(otherUserId, "Must not leak").Value);
        using var client = factory.CreateAuthenticatedClient(userId);

        var response = await client.GetAsync("/api/notifications/me");
        var body = await response.Content.ReadAsStringAsync();
        using var json = JsonDocument.Parse(body);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        json.RootElement.GetArrayLength().Should().Be(1);
        json.RootElement[0].GetProperty("userId").GetGuid().Should().Be(userId);
        json.RootElement[0].GetProperty("message").GetString().Should().Be("Visible notification");
    }

    [Fact]
    public async Task User_cannot_mark_another_users_notification_as_read()
    {
        await factory.ResetDatabaseAsync();
        var ownerId = Guid.NewGuid();
        var attackerId = Guid.NewGuid();
        var notification = NotificationItem.Create(ownerId, "Owner-only notification").Value;
        await SeedNotificationsAsync(notification);
        using var client = factory.CreateAuthenticatedClient(attackerId);

        var response = await client.PostAsync($"/api/notifications/{notification.Id}/read", null);
        var body = await response.Content.ReadAsStringAsync();
        using var problem = JsonDocument.Parse(body);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/problem+json");
        problem.RootElement.GetProperty("code").GetString().Should().Be("notification.mark_read_failed");
        problem.RootElement.GetProperty("traceId").GetString().Should().NotBeNullOrWhiteSpace();

        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<NotificationDbContext>();
        (await db.Notifications.AsNoTracking().SingleAsync(item => item.Id == notification.Id))
            .IsRead.Should().BeFalse();
    }

    [Fact]
    public async Task Read_all_only_mutates_the_authenticated_users_rows()
    {
        await factory.ResetDatabaseAsync();
        var userId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();
        var own = NotificationItem.Create(userId, "Own notification").Value;
        var other = NotificationItem.Create(otherUserId, "Other notification").Value;
        await SeedNotificationsAsync(own, other);
        using var client = factory.CreateAuthenticatedClient(userId);

        var response = await client.PostAsync("/api/notifications/read-all", null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<NotificationDbContext>();
        var states = await db.Notifications.AsNoTracking().ToDictionaryAsync(item => item.Id, item => item.IsRead);
        states[own.Id].Should().BeTrue();
        states[other.Id].Should().BeFalse();
    }

    private async Task SeedNotificationsAsync(params NotificationItem[] notifications)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<NotificationDbContext>();
        db.Notifications.AddRange(notifications);
        await db.SaveChangesAsync();
    }
}
