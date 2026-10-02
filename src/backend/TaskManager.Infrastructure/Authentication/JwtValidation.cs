using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace TaskManager.Infrastructure.Authentication;

public static class JwtValidation
{
    public static TokenValidationParameters Create(JwtOptions options)
    {
        // Keep issuance and validation subject to the same M3 configuration limits.
        _ = new JwtTokenService(Options.Create(options));
        return new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Convert.FromBase64String(options.SigningKey)),
            ValidAlgorithms = [SecurityAlgorithms.HmacSha256],
            ValidateIssuer = true, ValidIssuer = options.Issuer,
            ValidateAudience = true, ValidAudience = options.Audience,
            ValidateLifetime = true, RequireExpirationTime = true, RequireSignedTokens = true,
            ClockSkew = TimeSpan.Zero, NameClaimType = "unique_name"
        };
    }
}
