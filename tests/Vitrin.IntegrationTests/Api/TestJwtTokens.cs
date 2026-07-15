using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.IdentityModel.Tokens;
using Vitrin.Shared.Infrastructure.Auth;

namespace Vitrin.IntegrationTests.Api;

internal static class TestJwtTokens
{
    public static HttpClient CreateAuthenticatedClient<TEntryPoint>(
        this WebApplicationFactory<TEntryPoint> factory,
        Guid userId,
        string role = "Member")
        where TEntryPoint : class
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            Create(userId, role));
        return client;
    }

    private static string Create(Guid userId, string role)
    {
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, userId.ToString()),
            new Claim(JwtRegisteredClaimNames.Name, $"integration-{userId:N}"),
            new Claim(VitrinAuthDefaults.RoleClaim, role)
        };
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(NotificationApiFactory.JwtSecret));
        var token = new JwtSecurityToken(
            issuer: "Vitrin",
            audience: "Vitrin",
            claims: claims,
            notBefore: DateTime.UtcNow.AddMinutes(-1),
            expires: DateTime.UtcNow.AddMinutes(10),
            signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256));

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
