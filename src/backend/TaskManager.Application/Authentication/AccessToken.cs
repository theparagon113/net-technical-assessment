namespace TaskManager.Application.Authentication;

public sealed record AccessToken(string Value, DateTimeOffset ExpiresAt)
{
    public override string ToString() => nameof(AccessToken);
}
