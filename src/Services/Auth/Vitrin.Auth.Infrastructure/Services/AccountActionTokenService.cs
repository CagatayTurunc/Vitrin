using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Vitrin.Auth.Application.Interfaces;
using Vitrin.Auth.Domain.Entities;

namespace Vitrin.Auth.Infrastructure.Services;

public sealed class AccountActionTokenService : IAccountActionTokenService
{
    private readonly byte[] _secret;

    public AccountActionTokenService(IConfiguration configuration)
    {
        var configuredEmailSecret = configuration["Email:TokenSecret"];
        var secret = string.IsNullOrWhiteSpace(configuredEmailSecret)
            ? configuration["Jwt:Secret"]
            : configuredEmailSecret;
        if (string.IsNullOrWhiteSpace(secret) || secret.Length < 32)
            throw new InvalidOperationException("Email token secret must contain at least 32 characters.");

        _secret = Encoding.UTF8.GetBytes(secret);
    }

    public string Generate(User user, AccountActionPurpose purpose, TimeSpan lifetime)
    {
        var payload = new AccountActionTokenClaims(
            user.Id,
            purpose,
            DateTimeOffset.UtcNow.Add(lifetime),
            CreateSecurityStamp(user, purpose));
        var payloadBytes = JsonSerializer.SerializeToUtf8Bytes(payload);
        var signature = HMACSHA256.HashData(_secret, payloadBytes);
        return $"{Base64UrlEncode(payloadBytes)}.{Base64UrlEncode(signature)}";
    }

    public bool TryValidate(
        string token,
        AccountActionPurpose purpose,
        out AccountActionTokenClaims? claims)
    {
        claims = null;
        if (string.IsNullOrWhiteSpace(token)) return false;

        var parts = token.Split('.', 2);
        if (parts.Length != 2 || !TryBase64UrlDecode(parts[0], out var payloadBytes) ||
            !TryBase64UrlDecode(parts[1], out var suppliedSignature))
            return false;

        var expectedSignature = HMACSHA256.HashData(_secret, payloadBytes);
        if (suppliedSignature.Length != expectedSignature.Length ||
            !CryptographicOperations.FixedTimeEquals(suppliedSignature, expectedSignature))
            return false;

        try
        {
            claims = JsonSerializer.Deserialize<AccountActionTokenClaims>(payloadBytes);
            return claims is not null &&
                   claims.Purpose == purpose &&
                   claims.ExpiresAt > DateTimeOffset.UtcNow;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    public bool MatchesUser(AccountActionTokenClaims claims, User user)
    {
        var expectedStamp = CreateSecurityStamp(user, claims.Purpose);
        return claims.UserId == user.Id &&
               CryptographicOperations.FixedTimeEquals(
                   Encoding.UTF8.GetBytes(claims.SecurityStamp),
                   Encoding.UTF8.GetBytes(expectedStamp));
    }

    private static string CreateSecurityStamp(User user, AccountActionPurpose purpose)
    {
        var source = purpose == AccountActionPurpose.ResetPassword
            ? $"{user.Id:N}|{user.Email}|{user.PasswordHash}"
            : $"{user.Id:N}|{user.Email}|{user.CreatedAt.Ticks}|{user.EmailConfirmedAtUtc?.Ticks}";
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(source)));
    }

    private static string Base64UrlEncode(byte[] value) =>
        Convert.ToBase64String(value).TrimEnd('=').Replace('+', '-').Replace('/', '_');

    private static bool TryBase64UrlDecode(string value, out byte[] bytes)
    {
        try
        {
            var padded = value.Replace('-', '+').Replace('_', '/');
            padded += (padded.Length % 4) switch { 2 => "==", 3 => "=", _ => string.Empty };
            bytes = Convert.FromBase64String(padded);
            return true;
        }
        catch (FormatException)
        {
            bytes = [];
            return false;
        }
    }
}
