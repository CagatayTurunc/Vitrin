using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace Vitrin.Shared.Infrastructure.Auth;

public static class VitrinAuthDefaults
{
    public const string RoleClaim = "Role";
    public const string FullNameClaim = "FullName";
    public const string AvatarUrlClaim = "AvatarUrl";

    public const string AdminPolicy = "AdminOnly";
    public const string MakerOrAdminPolicy = "MakerOrAdmin";
}

public static class ClaimsPrincipalExtensions
{
    public static Guid? GetUserId(this ClaimsPrincipal principal)
    {
        if (principal.Identity?.IsAuthenticated != true)
        {
            return null;
        }

        var subject = principal.FindFirstValue(JwtRegisteredClaimNames.Sub)
            ?? principal.FindFirstValue(ClaimTypes.NameIdentifier);

        return Guid.TryParse(subject, out var userId) ? userId : null;
    }

    public static string GetUsername(this ClaimsPrincipal principal)
    {
        return principal.FindFirstValue(JwtRegisteredClaimNames.Name)
            ?? principal.FindFirstValue(ClaimTypes.Name)
            ?? principal.FindFirstValue(VitrinAuthDefaults.FullNameClaim)
            ?? "Kullanıcı";
    }
}
