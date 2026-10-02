namespace TaskManager.Infrastructure.Authentication;

public sealed class JwtOptions
{
    public const string SectionName = "Jwt";
    public string Issuer { get; set; } = "";
    public string Audience { get; set; } = "";
    // Base64-encoded random bytes, supplied by local secrets/environment configuration.
    public string SigningKey { get; set; } = "";
    public int LifetimeMinutes { get; set; } = 15;
}
