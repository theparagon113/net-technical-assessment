using TaskManager.Application.Authentication;
using TaskManager.Application.Users;
using TaskManager.Infrastructure.Authentication;
using TaskManager.Infrastructure.Persistence;
using TaskManager.Infrastructure.Persistence.Repositories;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Cryptography;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using TaskManager.Domain;

namespace TaskManager.Infrastructure.Tests;

public sealed class AuthenticationTests
{
    private static JwtOptions Configuration() => new()
    {
        Issuer = "assessment-tests",
        Audience = "assessment-client",
        SigningKey = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)),
        LifetimeMinutes = 15
    };

    [Fact]
    public void Framework_hasher_salts_hashes_and_verifies_passwords()
    {
        var hasher = new FrameworkPasswordHasher();
        var first = hasher.Hash("correct password");
        Assert.NotEqual("correct password", first);
        Assert.NotEqual(first, hasher.Hash("correct password"));
        Assert.True(hasher.Verify("correct password", first));
        Assert.False(hasher.Verify("incorrect password", first));
        Assert.False(hasher.Verify("correct password", "opaque legacy hash"));
        Assert.False(hasher.Verify("correct password", Convert.ToBase64String([1, 2, 3])));
    }

    [Fact]
    public void Jwt_signature_identity_issuer_audience_and_expiration_are_validated()
    {
        var config = Configuration();
        var service = new JwtTokenService(Options.Create(config));
        var result = service.Create(new(42, "Daniel", "sensitive hash"));
        var handler = new JwtSecurityTokenHandler { MapInboundClaims = false };
        var validation = new TokenValidationParameters
        {
            ValidateIssuer = true, ValidIssuer = config.Issuer,
            ValidateAudience = true, ValidAudience = config.Audience,
            ValidateLifetime = true, RequireExpirationTime = true,
            RequireSignedTokens = true, ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Convert.FromBase64String(config.SigningKey)),
            ValidAlgorithms = [SecurityAlgorithms.HmacSha256], ClockSkew = TimeSpan.Zero
        };
        var principal = handler.ValidateToken(result.Value, validation, out var token);
        Assert.Equal("42", principal.FindFirst("sub")?.Value);
        Assert.Equal("Daniel", principal.FindFirst("unique_name")?.Value);
        Assert.Equal(result.ExpiresAt.UtcDateTime, token.ValidTo);
        Assert.Equal(TimeSpan.FromMinutes(15), token.ValidTo - token.ValidFrom);
        Assert.DoesNotContain("sensitive", handler.ReadJwtToken(result.Value).Payload.SerializeToJson());
        Assert.DoesNotContain(handler.ReadJwtToken(result.Value).Claims,
            claim => claim.Type.Contains("password", StringComparison.OrdinalIgnoreCase));

        validation.ValidIssuer = "wrong-issuer";
        Assert.Throws<SecurityTokenInvalidIssuerException>(() => handler.ValidateToken(result.Value, validation, out _));
        validation.ValidIssuer = config.Issuer;
        validation.ValidAudience = "wrong-audience";
        Assert.Throws<SecurityTokenInvalidAudienceException>(() => handler.ValidateToken(result.Value, validation, out _));
        validation.ValidAudience = config.Audience;
        validation.IssuerSigningKey = new SymmetricSecurityKey(RandomNumberGenerator.GetBytes(32));
        Assert.ThrowsAny<SecurityTokenException>(() => handler.ValidateToken(result.Value, validation, out _));
        validation.IssuerSigningKey = new SymmetricSecurityKey(Convert.FromBase64String(config.SigningKey));
        var expired = new JwtTokenService(Options.Create(config), new FixedClock(DateTimeOffset.UtcNow.AddHours(-1)))
            .Create(new(42, "Daniel", "opaque"));
        Assert.Throws<SecurityTokenExpiredException>(() => handler.ValidateToken(expired.Value, validation, out _));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(61)]
    public void Invalid_lifetime_rejected(int lifetime)
    {
        var config = Configuration();
        config.LifetimeMinutes = lifetime;
        Assert.Throws<ArgumentOutOfRangeException>(() => new JwtTokenService(Options.Create(config)));
    }

    [Theory]
    [InlineData("")]
    [InlineData("not-base64")]
    [InlineData("YWJj")]
    public void Missing_malformed_or_short_signing_key_rejected(string key)
    {
        var config = Configuration();
        config.SigningKey = key;
        Assert.ThrowsAny<ArgumentException>(() => new JwtTokenService(Options.Create(config)));
    }

    [Fact]
    public void Missing_issuer_or_audience_rejected()
    {
        var config = Configuration();
        config.Issuer = " ";
        Assert.ThrowsAny<ArgumentException>(() => new JwtTokenService(Options.Create(config)));
        config.Issuer = "issuer";
        config.Audience = "";
        Assert.ThrowsAny<ArgumentException>(() => new JwtTokenService(Options.Create(config)));
    }

    [Theory]
    [InlineData("daniel")]
    [InlineData("DANIEL")]
    [InlineData(" DaNiEl ")]
    public async Task Real_sqlite_registration_login_and_duplicate_behavior(string variant)
    {
        using var database = new TestDatabase();
        await database.Initializer.InitializeAsync();
        var hasher = new FrameworkPasswordHasher();
        var service = new AuthService(database.Users, hasher, new JwtTokenService(Options.Create(Configuration())));
        var registered = await service.RegisterAsync(new(" Daniel ", "password!"));
        var login = await service.LoginAsync(new(variant, "password!"));
        Assert.Equal(registered.UserId, login.UserId);
        Assert.Equal("Daniel", login.Username);
        await Assert.ThrowsAsync<DuplicateUsernameException>(() => service.RegisterAsync(new(variant, "password!")));
        var stored = await database.Users.GetByIdAsync(registered.UserId, default);
        Assert.NotEqual("password!", stored!.PasswordHash);
        Assert.True(hasher.Verify("password!", stored.PasswordHash));
        await Assert.ThrowsAsync<InvalidCredentialsException>(() => service.LoginAsync(new(variant, "incorrect")));
    }

    [Fact]
    public async Task Demo_hash_enables_login_and_reruns_preserve_credentials_and_deleted_tasks()
    {
        using var database = new TestDatabase();
        await database.Initializer.InitializeAsync();
        var hasher = new FrameworkPasswordHasher();
        var seeder = new SqliteDemoSeeder(database.Connections);
        var hash = hasher.Hash(SqliteDemoSeeder.Password);
        await seeder.SeedAsync(hash);
        var service = new AuthService(database.Users, hasher, new JwtTokenService(Options.Create(Configuration())));
        var demo = await service.LoginAsync(new(" DEMO ", SqliteDemoSeeder.Password));
        var tasks = await database.Tasks.GetByUserIdAsync(demo.UserId, default);
        await database.Tasks.DeleteAsync(tasks[0].Id, demo.UserId, default);
        await seeder.SeedAsync(hasher.Hash("different password"));
        Assert.Equal(hash, (await database.Users.GetByIdAsync(demo.UserId, default))!.PasswordHash);
        Assert.Equal(2, (await database.Tasks.GetByUserIdAsync(demo.UserId, default)).Count);
        await service.LoginAsync(new("demo", SqliteDemoSeeder.Password));
    }

    [Fact]
    public async Task Real_insert_race_maps_to_duplicate_without_issuing_token()
    {
        using var database = new TestDatabase();
        await database.Initializer.InitializeAsync();
        var tokenService = new RecordingTokens();
        var service = new AuthService(new RacingUsers(database.Users), new FrameworkPasswordHasher(), tokenService);
        await Assert.ThrowsAsync<DuplicateUsernameException>(() => service.RegisterAsync(new("Daniel", "password!")));
        Assert.False(tokenService.Called);
        Assert.Equal("daniel", (await database.Users.GetByUsernameAsync("DANIEL", default))!.Username);
    }

    private sealed class RacingUsers(IUserRepository repository) : IUserRepository
    {
        public async Task<User> AddAsync(User user, CancellationToken token)
        {
            // Deterministic interleaving: another registration wins after the availability lookup.
            await repository.AddAsync(new(0, "daniel", "competing-hash"), token);
            return await repository.AddAsync(user, token);
        }
        public Task<User?> GetByUsernameAsync(string username, CancellationToken token) => repository.GetByUsernameAsync(username, token);
        public Task<User?> GetByIdAsync(int id, CancellationToken token) => repository.GetByIdAsync(id, token);
    }

    private sealed class RecordingTokens : ITokenService
    {
        public bool Called;
        public AccessToken Create(User user)
        {
            Called = true;
            throw new InvalidOperationException("A failed registration must not issue a token.");
        }
    }

    private sealed class FixedClock(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}
