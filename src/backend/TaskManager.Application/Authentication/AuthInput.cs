namespace TaskManager.Application.Authentication;

public sealed record AuthInput(string Username, string Password)
{
    public override string ToString() => nameof(AuthInput);
}
