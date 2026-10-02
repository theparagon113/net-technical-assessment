using TaskManager.Application.Authentication;
using Microsoft.Data.Sqlite;
using TaskManager.Domain;

namespace TaskManager.Infrastructure.Tests;

public sealed class UsernameIdentityTests : IDisposable
{
    private readonly TestDatabase database = new();

    [Theory]
    [InlineData("daniel")]
    [InlineData("DANIEL")]
    [InlineData(" Daniel ")]
    [InlineData("danIEL")]
    public async Task Database_rejects_case_equivalent_insert_without_auth_service(string variant)
    {
        await database.Initializer.InitializeAsync();
        var user = await database.Users.AddAsync(new(0, "Daniel", "opaque"), default);
        Assert.Equal(user.Id, (await database.Users.GetByUsernameAsync(variant, default))?.Id);
        await using var connection = await database.Connections.OpenAsync();
        using var command = connection.CreateCommand();
        command.CommandText = "INSERT INTO Users (Username, PasswordHash) VALUES (@name, 'opaque');";
        command.Parameters.AddWithValue("@name", variant);
        var error = await Assert.ThrowsAsync<SqliteException>(() => command.ExecuteNonQueryAsync());
        Assert.Equal(2067, error.SqliteExtendedErrorCode);
    }

    [Theory]
    [InlineData("Élodie", "éLODIE")]
    [InlineData("İpek", "İPEK")]
    public async Task Unicode_uses_the_same_ordinal_identity_in_database_and_application(string original, string variant)
    {
        await database.Initializer.InitializeAsync();
        var user = await database.Users.AddAsync(new(0, original, "opaque"), default);
        Assert.Equal(0, UsernamePolicy.Compare(original, variant));
        Assert.Equal(user.Id, (await database.Users.GetByUsernameAsync(variant, default))?.Id);
        await Assert.ThrowsAsync<DuplicateUsernameException>(() => database.Users.AddAsync(new(0, variant, "opaque"), default));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task M2_migration_preserves_records_and_refuses_identity_collisions(bool collision)
    {
        await using var connection = await database.Connections.OpenAsync();
        using var command = connection.CreateCommand();
        command.CommandText = """
            CREATE TABLE Users (Id INTEGER PRIMARY KEY AUTOINCREMENT, Username TEXT NOT NULL UNIQUE, PasswordHash TEXT NOT NULL);
            INSERT INTO Users (Username, PasswordHash) VALUES ('Daniel', 'original-hash');
            """;
        await command.ExecuteNonQueryAsync();
        if (collision)
        {
            command.CommandText = "INSERT INTO Users (Username, PasswordHash) VALUES ('daniel', 'other-hash');";
            await command.ExecuteNonQueryAsync();
            await Assert.ThrowsAsync<InvalidOperationException>(() => database.Initializer.InitializeAsync());
            command.CommandText = "SELECT COUNT(*) FROM Users;";
            Assert.Equal(2L, await command.ExecuteScalarAsync());
            command.CommandText = "SELECT COUNT(*) FROM sqlite_master WHERE name='IX_Users_UsernameIdentity';";
            Assert.Equal(0L, await command.ExecuteScalarAsync());
        }
        else
        {
            await database.Initializer.InitializeAsync();
            await database.Initializer.InitializeAsync();
            var user = await database.Users.GetByUsernameAsync("DANIEL", default);
            Assert.Equal(1, user!.Id);
            Assert.Equal("Daniel", user.Username);
            Assert.Equal("original-hash", user.PasswordHash);
            await Assert.ThrowsAsync<DuplicateUsernameException>(() => database.Users.AddAsync(new(0, "daniel", "opaque"), default));
        }
    }

    [Fact]
    public async Task Existing_uppercase_demo_is_not_reseeded_or_reset()
    {
        await database.Initializer.InitializeAsync();
        var user = await database.Users.AddAsync(new(0, "DEMO", "original"), default);
        await new TaskManager.Infrastructure.Persistence.SqliteDemoSeeder(database.Connections).SeedAsync("replacement");
        Assert.Equal("original", (await database.Users.GetByIdAsync(user.Id, default))!.PasswordHash);
        Assert.Empty(await database.Tasks.GetByUserIdAsync(user.Id, default));
    }

    public void Dispose() => database.Dispose();
}
