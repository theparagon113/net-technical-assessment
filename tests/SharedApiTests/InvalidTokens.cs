using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using Microsoft.IdentityModel.Tokens;

namespace TaskManager.SharedApiTests;

internal static class InvalidTokens
{
    public static string Create(TestSettings settings, string variant)
    {
        if (variant == "malformed") return "not-a-jwt";
        var key = variant == "signature" ? RandomNumberGenerator.GetBytes(48)
            : Convert.FromBase64String(settings.Values["Jwt:SigningKey"]!);
        var claims = new List<Claim> { new("unique_name", "test-user") };
        if (variant != "missing-sub") claims.Add(new("sub", variant switch
        {
            "zero-sub" => "0", "negative-sub" => "-1", "invalid-sub" => "abc", "overflow-sub" => "2147483648", _ => "1"
        }));
        if (variant == "duplicate-sub") claims.Add(new("sub", "2"));
        var now = DateTime.UtcNow;
        var token = new JwtSecurityToken(
            variant == "issuer" ? "wrong" : settings.Values["Jwt:Issuer"],
            variant == "audience" ? "wrong" : settings.Values["Jwt:Audience"], claims,
            variant == "future" ? now.AddMinutes(1) : now.AddHours(-1), variant == "expired" ? now.AddSeconds(-1) : now.AddMinutes(5),
            variant == "unsigned" ? null : new SigningCredentials(new SymmetricSecurityKey(key),
                variant == "algorithm" ? SecurityAlgorithms.HmacSha384 : SecurityAlgorithms.HmacSha256));
        if (variant == "missing-exp") token.Payload.Remove("exp");
        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
