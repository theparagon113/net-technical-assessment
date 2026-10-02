namespace TaskManager.Application.Authentication;

public sealed record AuthResult(int UserId, string Username, string AccessToken, DateTimeOffset ExpiresAt)
{
    public override string ToString() => $"AuthResult {{ UserId = {UserId}, Username = {Username} }}";
}
