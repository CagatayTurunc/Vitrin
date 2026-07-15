using System.Text.Json;
using FluentAssertions;

namespace Vitrin.IntegrationTests.Api;

[Trait("Category", "Contract")]
public sealed class OpenApiCompatibilityTests(NotificationApiFactory factory)
    : IClassFixture<NotificationApiFactory>
{
    [Fact]
    public async Task Notification_openapi_keeps_all_approved_operations()
    {
        await factory.ResetDatabaseAsync();
        using var client = factory.CreateClient();
        using var actual = JsonDocument.Parse(await client.GetStringAsync("/swagger/v1/swagger.json"));
        using var baseline = JsonDocument.Parse(
            await File.ReadAllTextAsync(Path.Combine(AppContext.BaseDirectory, "Contracts", "notification.openapi-baseline.json")));

        var actualPaths = actual.RootElement.GetProperty("paths");
        foreach (var operation in baseline.RootElement.GetProperty("operations").EnumerateArray())
        {
            var path = operation.GetProperty("path").GetString()!;
            var method = operation.GetProperty("method").GetString()!;
            var operationId = operation.GetProperty("operationId").GetString()!;

            actualPaths.TryGetProperty(path, out var pathDefinition)
                .Should().BeTrue($"the approved path {path} must remain available");
            pathDefinition.TryGetProperty(method, out var methodDefinition)
                .Should().BeTrue($"the approved operation {method.ToUpperInvariant()} {path} must remain available");
            methodDefinition.GetProperty("operationId").GetString().Should().Be(operationId);
        }
    }
}
