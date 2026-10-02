using TaskManager.Application.Authentication;
using TaskManager.Application.Users;
using TaskManager.Domain;
using Microsoft.Data.Sqlite;

namespace TaskManager.Infrastructure.Persistence.Repositories;

public sealed class SqliteUserRepository(SqliteConnectionFactory connections) : IUserRepository
{
    public async Task<User> AddAsync(User user, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(user);
        if (user.Id != 0)
            throw new ArgumentException("Only an unsaved user can be inserted.", nameof(user));
        await using var connection = await connections.OpenAsync(cancellationToken);
        using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO Users (Username, PasswordHash) VALUES (@username, @passwordHash)
            RETURNING Id;
            """;
        command.Parameters.AddWithValue("@username", user.Username);
        command.Parameters.AddWithValue("@passwordHash", user.PasswordHash);
        try
        {
            var id = checked((int)(long)(await command.ExecuteScalarAsync(cancellationToken))!);
            return new User(id, user.Username, user.PasswordHash);
        }
        catch (SqliteException error) when (error.SqliteExtendedErrorCode == 2067)
        {
            throw new DuplicateUsernameException();
        }
    }

    public async Task<User?> GetByIdAsync(int userId, CancellationToken cancellationToken)
    {
        await using var connection = await connections.OpenAsync(cancellationToken);
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT Id, Username, PasswordHash FROM Users WHERE Id = @id;";
        command.Parameters.AddWithValue("@id", userId);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        return await reader.ReadAsync(cancellationToken) ? Map(reader) : null;
    }

    public async Task<User?> GetByUsernameAsync(string username, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(username);
        await using var connection = await connections.OpenAsync(cancellationToken);
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT Id, Username, PasswordHash FROM Users WHERE Username COLLATE USERNAME_IDENTITY = @username;";
        command.Parameters.AddWithValue("@username", username.Trim());
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        return await reader.ReadAsync(cancellationToken) ? Map(reader) : null;
    }

    private static User Map(SqliteDataReader reader) =>
        new(reader.GetInt32(0), reader.GetString(1), reader.GetString(2));
}
