using System.Net;
using System.Net.Http.Headers;
using System.Text;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Vitrin.Auth.Domain.Entities;
using Vitrin.Auth.Infrastructure.Services;
using Xunit;

namespace Vitrin.Auth.Tests.Infrastructure;

public class ExternalIdentityVerifierTests
{
    [Fact]
    public async Task VerifyAsync_WithValidGoogleTokenInfo_ShouldReturnVerifiedIdentity()
    {
        const string clientId = "google-client-id";
        using var httpClient = CreateClient(request =>
        {
            request.RequestUri!.Host.Should().Be("oauth2.googleapis.com");
            request.RequestUri.Query.Should().Contain("id_token=provider-token");

            return JsonResponse($$"""
                {
                  "sub": "google-subject",
                  "email": "Verified@Example.com",
                  "email_verified": "true",
                  "aud": "{{clientId}}",
                  "iss": "https://accounts.google.com",
                  "name": "Verified User",
                  "picture": "https://example.com/avatar.png"
                }
                """);
        });
        using var verifier = new ExternalIdentityVerifier(Configuration(clientId), httpClient);

        var result = await verifier.VerifyAsync(
            AuthProvider.Google,
            "provider-token",
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.ProviderId.Should().Be("google-subject");
        result.Value.Email.Should().Be("verified@example.com");
    }

    [Fact]
    public async Task VerifyAsync_WithWrongGoogleAudience_ShouldRejectIdentity()
    {
        using var httpClient = CreateClient(_ => JsonResponse("""
            {
              "sub": "google-subject",
              "email": "verified@example.com",
              "email_verified": "true",
              "aud": "attacker-client-id",
              "iss": "https://accounts.google.com"
            }
            """));
        using var verifier = new ExternalIdentityVerifier(
            Configuration("expected-client-id"),
            httpClient);

        var result = await verifier.VerifyAsync(
            AuthProvider.Google,
            "provider-token",
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task VerifyAsync_WithGithubToken_ShouldUseVerifiedPrimaryEmail()
    {
        using var httpClient = CreateClient(request =>
        {
            request.Headers.Authorization.Should().BeEquivalentTo(
                new AuthenticationHeaderValue("Bearer", "github-access-token"));

            return request.RequestUri!.AbsolutePath switch
            {
                "/user" => JsonResponse("""
                    {
                      "id": 12345,
                      "login": "octocat",
                      "name": "Octo Cat",
                      "avatar_url": "https://example.com/octocat.png"
                    }
                    """),
                "/user/emails" => JsonResponse("""
                    [
                      { "email": "unverified@example.com", "primary": false, "verified": false },
                      { "email": "primary@example.com", "primary": true, "verified": true }
                    ]
                    """),
                _ => new HttpResponseMessage(HttpStatusCode.NotFound)
            };
        });
        using var verifier = new ExternalIdentityVerifier(Configuration(), httpClient);

        var result = await verifier.VerifyAsync(
            AuthProvider.Github,
            "github-access-token",
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.ProviderId.Should().Be("12345");
        result.Value.Email.Should().Be("primary@example.com");
        result.Value.FullName.Should().Be("Octo Cat");
    }

    private static IConfiguration Configuration(string? googleClientId = null) =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["OAuth:Google:ClientId"] = googleClientId
            })
            .Build();

    private static HttpClient CreateClient(
        Func<HttpRequestMessage, HttpResponseMessage> responder) =>
        new(new StubHttpMessageHandler(responder));

    private static HttpResponseMessage JsonResponse(string json) =>
        new(HttpStatusCode.OK)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };

    private sealed class StubHttpMessageHandler(
        Func<HttpRequestMessage, HttpResponseMessage> responder) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken) =>
            Task.FromResult(responder(request));
    }
}
