using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using FluentAssertions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Vitrin.Shared.Infrastructure.Auth;
using Xunit;

namespace Vitrin.Auth.Tests.Infrastructure;

public class VitrinAuthenticationExtensionsTests
{
    [Fact]
    public async Task AddVitrinJwtAuthentication_ShouldRegisterAdminAndMakerPolicies()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Secret"] = "a-secure-test-secret-that-is-at-least-32-bytes",
                ["Jwt:Issuer"] = "Vitrin",
                ["Jwt:Audience"] = "Vitrin"
            })
            .Build();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddVitrinJwtAuthentication(configuration);

        await using var provider = services.BuildServiceProvider();
        var policyProvider = provider.GetRequiredService<IAuthorizationPolicyProvider>();

        var adminPolicy = await policyProvider.GetPolicyAsync(VitrinAuthDefaults.AdminPolicy);
        var makerPolicy = await policyProvider.GetPolicyAsync(VitrinAuthDefaults.MakerOrAdminPolicy);

        adminPolicy.Should().NotBeNull();
        adminPolicy!.Requirements
            .OfType<RolesAuthorizationRequirement>()
            .Single()
            .AllowedRoles.Should().Equal("Admin");
        makerPolicy.Should().NotBeNull();
        makerPolicy!.Requirements
            .OfType<RolesAuthorizationRequirement>()
            .Single()
            .AllowedRoles.Should().BeEquivalentTo(["Maker", "Admin"]);
    }

    [Fact]
    public void GetUserId_ShouldOnlyReadAuthenticatedPrincipalClaims()
    {
        var userId = Guid.NewGuid();
        var principal = new ClaimsPrincipal(new ClaimsIdentity(
            [new Claim(JwtRegisteredClaimNames.Sub, userId.ToString())],
            "Bearer"));

        principal.GetUserId().Should().Be(userId);
        new ClaimsPrincipal(new ClaimsIdentity()).GetUserId().Should().BeNull();
        new ClaimsPrincipal(new ClaimsIdentity(
            [new Claim(JwtRegisteredClaimNames.Sub, userId.ToString())]))
            .GetUserId().Should().BeNull();
    }

    [Fact]
    public void AddVitrinJwtAuthentication_WithShortSecret_ShouldFailFast()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Secret"] = "too-short"
            })
            .Build();

        Action action = () => new ServiceCollection()
            .AddVitrinJwtAuthentication(configuration);

        action.Should().Throw<InvalidOperationException>();
    }
}
