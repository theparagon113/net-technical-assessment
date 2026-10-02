using TaskManager.Application.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace TaskManager.Infrastructure.Authentication;

public sealed class FrameworkPasswordHasher : TaskManager.Application.Authentication.IPasswordHasher
{
    private readonly PasswordHasher<object> hasher = new(Options.Create(new PasswordHasherOptions
    {
        CompatibilityMode = PasswordHasherCompatibilityMode.IdentityV3,
        IterationCount = 210_000
    }));
    private readonly object context = new();

    public string Hash(string password)
    {
        PasswordPolicy.Validate(password);
        return hasher.HashPassword(context, password);
    }

    public bool Verify(string password, string passwordHash)
    {
        PasswordPolicy.Validate(password);
        if (string.IsNullOrWhiteSpace(passwordHash)) return false;
        try
        {
            return hasher.VerifyHashedPassword(context, passwordHash, password) != PasswordVerificationResult.Failed;
        }
        catch (FormatException)
        {
            // Malformed legacy storage must fail authentication rather than leak parsing errors.
            return false;
        }
    }
}
