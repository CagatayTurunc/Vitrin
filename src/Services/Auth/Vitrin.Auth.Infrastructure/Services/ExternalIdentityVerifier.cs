using System.Globalization;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Configuration;
using Vitrin.Auth.Application.Interfaces;
using Vitrin.Auth.Domain.Entities;
using Vitrin.Shared.Kernel.Results;

namespace Vitrin.Auth.Infrastructure.Services;

public sealed class ExternalIdentityVerifier : IExternalIdentityVerifier, IDisposable
{
    private static readonly Uri GithubUserEndpoint = new("https://api.github.com/user");
    private static readonly Uri GithubEmailsEndpoint = new("https://api.github.com/user/emails");

    private readonly HttpClient _httpClient;
    private readonly string? _googleClientId;

    public ExternalIdentityVerifier(IConfiguration configuration, HttpClient httpClient)
    {
        _googleClientId = configuration["OAuth:Google:ClientId"];
        _httpClient = httpClient;
    }

    public Task<Result<VerifiedExternalIdentity>> VerifyAsync(
        AuthProvider provider,
        string providerToken,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(providerToken) || providerToken.Length > 16_384)
        {
            return Task.FromResult(Result<VerifiedExternalIdentity>.Failure(
                "External provider token is invalid."));
        }

        return provider switch
        {
            AuthProvider.Google => VerifyGoogleAsync(providerToken, cancellationToken),
            AuthProvider.Github => VerifyGithubAsync(providerToken, cancellationToken),
            _ => Task.FromResult(Result<VerifiedExternalIdentity>.Failure(
                "Unsupported external authentication provider."))
        };
    }

    private async Task<Result<VerifiedExternalIdentity>> VerifyGoogleAsync(
        string idToken,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_googleClientId))
        {
            return Result<VerifiedExternalIdentity>.Failure(
                "Google authentication is not configured.");
        }

        try
        {
            var endpoint = new Uri(
                $"https://oauth2.googleapis.com/tokeninfo?id_token={Uri.EscapeDataString(idToken)}");
            using var response = await _httpClient.GetAsync(endpoint, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                return VerificationFailed();
            }

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            var payload = JsonSerializer.Deserialize<GoogleTokenInfo>(json);
            var validIssuer = payload?.Issuer is "accounts.google.com" or "https://accounts.google.com";

            if (payload is null
                || payload.Audience != _googleClientId
                || !validIssuer
                || !string.Equals(payload.EmailVerified, "true", StringComparison.OrdinalIgnoreCase)
                || string.IsNullOrWhiteSpace(payload.Subject)
                || string.IsNullOrWhiteSpace(payload.Email))
            {
                return VerificationFailed();
            }

            return Result<VerifiedExternalIdentity>.Success(new VerifiedExternalIdentity(
                payload.Subject,
                payload.Email.Trim().ToLowerInvariant(),
                string.IsNullOrWhiteSpace(payload.Name) ? payload.Email : payload.Name,
                payload.Picture ?? string.Empty));
        }
        catch (Exception exception) when (
            exception is HttpRequestException or TaskCanceledException or JsonException)
        {
            return VerificationFailed();
        }
    }

    private async Task<Result<VerifiedExternalIdentity>> VerifyGithubAsync(
        string accessToken,
        CancellationToken cancellationToken)
    {
        try
        {
            using var userRequest = CreateGithubRequest(GithubUserEndpoint, accessToken);
            using var userResponse = await _httpClient.SendAsync(userRequest, cancellationToken);
            if (!userResponse.IsSuccessStatusCode)
            {
                return VerificationFailed();
            }

            var userJson = await userResponse.Content.ReadAsStringAsync(cancellationToken);
            var user = JsonSerializer.Deserialize<GithubUser>(userJson);
            if (user is null || user.Id <= 0 || string.IsNullOrWhiteSpace(user.Login))
            {
                return VerificationFailed();
            }

            using var emailRequest = CreateGithubRequest(GithubEmailsEndpoint, accessToken);
            using var emailResponse = await _httpClient.SendAsync(emailRequest, cancellationToken);
            if (!emailResponse.IsSuccessStatusCode)
            {
                return VerificationFailed();
            }

            var emailJson = await emailResponse.Content.ReadAsStringAsync(cancellationToken);
            var emails = JsonSerializer.Deserialize<List<GithubEmail>>(emailJson) ?? [];
            var email = emails.FirstOrDefault(item => item.Primary && item.Verified)
                ?? emails.FirstOrDefault(item => item.Verified);

            if (email is null || string.IsNullOrWhiteSpace(email.Email))
            {
                return VerificationFailed();
            }

            return Result<VerifiedExternalIdentity>.Success(new VerifiedExternalIdentity(
                user.Id.ToString(CultureInfo.InvariantCulture),
                email.Email.Trim().ToLowerInvariant(),
                string.IsNullOrWhiteSpace(user.Name) ? user.Login : user.Name,
                user.AvatarUrl ?? string.Empty));
        }
        catch (Exception exception) when (
            exception is HttpRequestException or TaskCanceledException or JsonException)
        {
            return VerificationFailed();
        }
    }

    private static HttpRequestMessage CreateGithubRequest(Uri endpoint, string accessToken)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, endpoint);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));
        request.Headers.UserAgent.ParseAdd("Vitrin/1.0");
        request.Headers.Add("X-GitHub-Api-Version", "2022-11-28");
        return request;
    }

    private static Result<VerifiedExternalIdentity> VerificationFailed() =>
        Result<VerifiedExternalIdentity>.Failure("External identity could not be verified.");

    public void Dispose() => _httpClient.Dispose();

    private sealed record GoogleTokenInfo(
        [property: JsonPropertyName("sub")] string Subject,
        [property: JsonPropertyName("email")] string Email,
        [property: JsonPropertyName("email_verified")] string EmailVerified,
        [property: JsonPropertyName("aud")] string Audience,
        [property: JsonPropertyName("iss")] string Issuer,
        [property: JsonPropertyName("name")] string? Name,
        [property: JsonPropertyName("picture")] string? Picture);

    private sealed record GithubUser(
        [property: JsonPropertyName("id")] long Id,
        [property: JsonPropertyName("login")] string Login,
        [property: JsonPropertyName("name")] string? Name,
        [property: JsonPropertyName("avatar_url")] string? AvatarUrl);

    private sealed record GithubEmail(
        [property: JsonPropertyName("email")] string Email,
        [property: JsonPropertyName("primary")] bool Primary,
        [property: JsonPropertyName("verified")] bool Verified);
}
