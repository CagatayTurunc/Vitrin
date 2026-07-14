using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace Vitrin.Shared.Infrastructure.Auth;

public static class VitrinAuthenticationExtensions
{
    public static IServiceCollection AddVitrinJwtAuthentication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var secret = configuration["Jwt:Secret"];
        if (string.IsNullOrWhiteSpace(secret) || Encoding.UTF8.GetByteCount(secret) < 32)
        {
            throw new InvalidOperationException(
                "Jwt:Secret en az 32 bayt uzunluğunda yapılandırılmalıdır.");
        }

        var issuer = configuration["Jwt:Issuer"] ?? "Vitrin";
        var audience = configuration["Jwt:Audience"] ?? "Vitrin";

        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.MapInboundClaims = false;
                options.SaveToken = false;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = issuer,
                    ValidateAudience = true,
                    ValidAudience = audience,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.FromMinutes(1),
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret)),
                    ValidAlgorithms = [SecurityAlgorithms.HmacSha256],
                    NameClaimType = System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Name,
                    RoleClaimType = VitrinAuthDefaults.RoleClaim
                };
            });

        services.AddAuthorization(options =>
        {
            options.AddPolicy(VitrinAuthDefaults.AdminPolicy, policy =>
                policy.RequireAuthenticatedUser().RequireRole("Admin"));

            options.AddPolicy(VitrinAuthDefaults.MakerOrAdminPolicy, policy =>
                policy.RequireAuthenticatedUser().RequireRole("Maker", "Admin"));
        });

        return services;
    }
}
