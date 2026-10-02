using Microsoft.Data.Sqlite;
using TaskManager.Application.Authentication;

namespace TaskManager.Infrastructure.Persistence;

public sealed class SqliteConnectionFactory
{
    public const string UsernameCollation = "USERNAME_IDENTITY";
    private readonly string connectionString;

    public SqliteConnectionFactory(string connectionString)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);
        this.connectionString = new SqliteConnectionStringBuilder(connectionString)
        {
            ForeignKeys = true
        }.ToString();
    }

    public async Task<SqliteConnection> OpenAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var connection = new SqliteConnection(connectionString);
        try
        {
            await connection.OpenAsync(cancellationToken);
            connection.CreateCollation(UsernameCollation, UsernamePolicy.Compare);
            return connection;
        }
        catch
        {
            await connection.DisposeAsync();
            throw;
        }
    }
}
