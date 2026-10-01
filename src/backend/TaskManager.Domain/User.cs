namespace TaskManager.Domain;

// Persistence data only; hashing and authentication belong to the next milestone.
public sealed class User
{
    public int Id { get; }
    public string Username { get; }
    public string PasswordHash { get; }

    public User(int id, string username, string passwordHash)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(id);
        ArgumentException.ThrowIfNullOrWhiteSpace(username);
        ArgumentException.ThrowIfNullOrWhiteSpace(passwordHash);
        Id = id;
        Username = username.Trim();
        PasswordHash = passwordHash;
    }
}
