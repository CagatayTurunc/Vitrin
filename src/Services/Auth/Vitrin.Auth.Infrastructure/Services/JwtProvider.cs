using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Vitrin.Auth.Application.Interfaces;
using Vitrin.Auth.Domain.Entities;
using Vitrin.Shared.Infrastructure.Auth;

namespace Vitrin.Auth.Infrastructure.Services;

public class JwtProvider : IJwtProvider
{
    private readonly IConfiguration _configuration;

    public JwtProvider(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public string Generate(User user)
    {
        var claims = new List<Claim>
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim(JwtRegisteredClaimNames.Name, user.Username),
            new Claim(VitrinAuthDefaults.FullNameClaim, user.FullName),
            new Claim(VitrinAuthDefaults.AvatarUrlClaim, user.AvatarUrl),
            new Claim(VitrinAuthDefaults.RoleClaim, user.Role.ToString()),
            new Claim("vitrin:banned", user.IsBanned(DateTime.UtcNow) ? "true" : "false")
        };

        if (user.ActiveBanId.HasValue)
            claims.Add(new Claim("vitrin:ban_id", user.ActiveBanId.Value.ToString()));

        var secret = _configuration["Jwt:Secret"];
        if (string.IsNullOrWhiteSpace(secret) || Encoding.UTF8.GetByteCount(secret) < 32)
        {
            throw new InvalidOperationException("Jwt:Secret en az 32 bayt uzunluğunda yapılandırılmalıdır.");
        }
        var issuer = _configuration["Jwt:Issuer"] ?? "Vitrin";
        var audience = _configuration["Jwt:Audience"] ?? "Vitrin";

        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));

        var token = new JwtSecurityToken(
            issuer,
            audience,
            claims,
            null,
            DateTime.UtcNow.AddHours(24),
            new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256)
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
