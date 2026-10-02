using TaskManager.Application.Authentication;
using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using TaskManager.Domain;

namespace TaskManager.Infrastructure.Authentication;

public sealed class JwtTokenService : ITokenService
{
    private readonly string issuer;
    private readonly string audience;
    private readonly int lifetimeMinutes;
    private readonly SigningCredentials credentials;
    private readonly TimeProvider clock;

    public JwtTokenService(IOptions<JwtOptions> options, TimeProvider? clock = null)
    {
        ArgumentNullException.ThrowIfNull(options);
        var value = options.Value;
        ArgumentException.ThrowIfNullOrWhiteSpace(value.Issuer);
        ArgumentException.ThrowIfNullOrWhiteSpace(value.Audience);
        ArgumentException.ThrowIfNullOrWhiteSpace(value.SigningKey);
        if (value.LifetimeMinutes is < 1 or > 60)
            throw new ArgumentOutOfRangeException(nameof(options), "JWT lifetime must be 1–60 minutes.");
        byte[] key;
        try { key = Convert.FromBase64String(value.SigningKey); }
        catch (FormatException) { throw new ArgumentException("JWT signing key must be Base64-encoded random bytes.", nameof(options)); }
        if (key.Length < 32)
            throw new ArgumentException("JWT signing key must contain at least 32 random bytes.", nameof(options));
        issuer = value.Issuer;
        audience = value.Audience;
        lifetimeMinutes = value.LifetimeMinutes;
        credentials = new(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256);
        this.clock = clock ?? TimeProvider.System;
    }

    public AccessToken Create(User user)
    {
        ArgumentNullException.ThrowIfNull(user);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(user.Id);
        var now = DateTimeOffset.FromUnixTimeSeconds(clock.GetUtcNow().ToUnixTimeSeconds());
        var expires = now.AddMinutes(lifetimeMinutes);
        var token = new JwtSecurityToken(issuer, audience,
            [new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString(CultureInfo.InvariantCulture)),
             new Claim(JwtRegisteredClaimNames.UniqueName, user.Username)],
            now.UtcDateTime, expires.UtcDateTime, credentials);
        return new(new JwtSecurityTokenHandler().WriteToken(token), expires);
    }
}
