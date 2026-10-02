namespace TaskManager.Application.Authentication;

public static class PasswordPolicy
{
    public const int MinimumLength = 8;
    public const int MaximumLength = 128;

    public static void Validate(string password)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(password);
        if (password.Length is < MinimumLength or > MaximumLength)
            throw new ArgumentException("Password must contain 8–128 characters.", nameof(password));
    }
}
