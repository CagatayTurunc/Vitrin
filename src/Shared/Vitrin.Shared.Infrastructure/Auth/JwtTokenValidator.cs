using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace Vitrin.Shared.Infrastructure.Auth;

/// <summary>
/// İmza doğrulamalı JWT token yardımcısı.
/// Tüm mikroservislerde ReadJwtToken() (imzasız decode) yerine
/// bu sınıf kullanılmalıdır.
/// </summary>
public sealed class JwtTokenValidator
{
    private readonly TokenValidationParameters _parameters;

    public JwtTokenValidator(IConfiguration configuration)
    {
        var secret = configuration["Jwt:Secret"];
        if (string.IsNullOrWhiteSpace(secret) || Encoding.UTF8.GetByteCount(secret) < 32)
        {
            throw new InvalidOperationException(
                "Jwt:Secret en az 32 bayt uzunluğunda yapılandırılmalıdır.");
        }

        var issuer   = configuration["Jwt:Issuer"]   ?? "Vitrin";
        var audience = configuration["Jwt:Audience"] ?? "Vitrin";

        _parameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey         = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret)),
            ValidateIssuer           = true,
            ValidIssuer              = issuer,
            ValidateAudience         = true,
            ValidAudience            = audience,
            ValidateLifetime         = true,
            ClockSkew                = TimeSpan.Zero
        };
    }

    /// <summary>
    /// Token'ı doğrular. Başarısızsa null döner.
    /// </summary>
    public ClaimsPrincipal? Validate(string token)
    {
        try
        {
            var handler   = new JwtSecurityTokenHandler();
            var principal = handler.ValidateToken(token, _parameters, out _);
            return principal;
        }
        catch
        {
            return null;
        }
    }

    // ── Yardımcı metodlar ────────────────────────────────────────────────────

    /// <summary>
    /// Authorization header'dan kullanıcı ID'sini doğrulayarak çeker.
    /// Token geçersizse null döner.
    /// </summary>
    public Guid? GetUserId(HttpContext context)
    {
        var principal = GetPrincipal(context);
        if (principal is null) return null;

        var sub = principal.FindFirstValue(JwtRegisteredClaimNames.Sub)
               ?? principal.FindFirstValue(ClaimTypes.NameIdentifier);

        return Guid.TryParse(sub, out var id) ? id : null;
    }

    /// <summary>
    /// Authorization header'dan kullanıcı rolünü doğrulayarak çeker.
    /// Token geçersizse null döner.
    /// </summary>
    public string? GetRole(HttpContext context)
    {
        var principal = GetPrincipal(context);
        if (principal is null) return null;

        return principal.FindFirstValue("Role")
            ?? principal.FindFirstValue(ClaimTypes.Role);
    }

    /// <summary>
    /// Authorization header'dan ClaimsPrincipal döner.
    /// Token yoksa veya geçersizse null döner.
    /// </summary>
    public ClaimsPrincipal? GetPrincipal(HttpContext context)
    {
        var authHeader = context.Request.Headers["Authorization"].FirstOrDefault();
        if (authHeader is null || !authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            return null;

        var token = authHeader["Bearer ".Length..].Trim();
        return Validate(token);
    }
}
