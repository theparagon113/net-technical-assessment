using Microsoft.Data.Sqlite;

namespace TaskManager.Infrastructure.Persistence;

public sealed class SqliteDatabaseInitializer(SqliteConnectionFactory connections)
{
    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = await connections.OpenAsync(cancellationToken);
        using var transaction = connection.BeginTransaction();
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = """
            CREATE TABLE IF NOT EXISTS Users (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Username TEXT NOT NULL UNIQUE,
                PasswordHash TEXT NOT NULL
            );
            CREATE UNIQUE INDEX IF NOT EXISTS IX_Users_UsernameIdentity
                ON Users (Username COLLATE USERNAME_IDENTITY);
            CREATE TABLE IF NOT EXISTS Tasks (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                UserId INTEGER NOT NULL,
                Title TEXT NOT NULL,
                Description TEXT NULL,
                Status INTEGER NOT NULL,
                DueDate TEXT NOT NULL,
                FOREIGN KEY (UserId) REFERENCES Users(Id)
            );
            """;
        try
        {
            await command.ExecuteNonQueryAsync(cancellationToken);
        }
        catch (SqliteException error) when (error.SqliteExtendedErrorCode == 2067)
        {
            throw new InvalidOperationException(
                "Existing usernames conflict under M3 identity rules. Resolve account collisions before retrying initialization; no accounts were changed.", error);
        }
        await transaction.CommitAsync(cancellationToken);
    }
}
