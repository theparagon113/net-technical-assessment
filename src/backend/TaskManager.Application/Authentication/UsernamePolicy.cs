namespace TaskManager.Application.Authentication;

public static class UsernamePolicy
{
    public const int MinimumLength = 3;
    public const int MaximumLength = 64;

    public static string ValidateAndTrim(string username)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(username);
        var trimmed = username.Trim();
        if (trimmed.Length is < MinimumLength or > MaximumLength || trimmed.Any(char.IsControl))
            throw new ArgumentException("Username must contain 3–64 characters and no control characters.", nameof(username));
        return trimmed;
    }

    // One identity comparison is shared with SQLite, including for legacy untrimmed values.
    public static int Compare(string? left, string? right) =>
        StringComparer.OrdinalIgnoreCase.Compare(left?.Trim(), right?.Trim());
}
