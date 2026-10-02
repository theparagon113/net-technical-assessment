using System.Globalization;
using Microsoft.Data.Sqlite;
using TaskManager.Domain;
using TaskManager.Infrastructure.Persistence;
using TaskManager.Infrastructure.Persistence.Repositories;
using TaskManager.Application.Authentication;
using TaskStatus = TaskManager.Domain.TaskStatus;

namespace TaskManager.Infrastructure.Tests;

public sealed class SqlitePersistenceTests : IDisposable
{
    private readonly TestDatabase database = new();
    // Opaque persistence fixture, deliberately not a fabricated usable credential/hash.
    private const string HashFixture = "opaque-hash-fixture-for-storage-tests";
    private static readonly CancellationToken Token = CancellationToken.None;

    private async Task<User> AddUser(string username = "owner") =>
        await database.Users.AddAsync(new User(0, username, HashFixture), Token);

    private static TaskItem NewTask(int owner, string? description = null,
        TaskStatus status = TaskStatus.Pending) =>
        new(0, owner, "Task '; DROP TABLE Tasks; --", description, new DateOnly(2028, 2, 29), status);

    [Fact]
    public async Task User_insert_assigns_ids_and_reads_exact_hash_by_id_and_username()
    {
        await database.Initializer.InitializeAsync();
        const string username = "owner'; DROP TABLE Users; --";
        var input = new User(0, username, HashFixture);
        var saved = await database.Users.AddAsync(input, Token);
        var second = await AddUser("second");
        Assert.Equal(0, input.Id);
        Assert.True(saved.Id > 0);
        Assert.True(second.Id > saved.Id);
        var byId = await database.Users.GetByIdAsync(saved.Id, Token);
        var byName = await database.Users.GetByUsernameAsync(username, Token);
        Assert.Equal(username, byId!.Username);
        Assert.Equal(saved.Id, byName!.Id);
        Assert.Equal(HashFixture, byId.PasswordHash);
        Assert.Equal(HashFixture, byName.PasswordHash);
        Assert.Null(await database.Users.GetByIdAsync(999, Token));
        Assert.Null(await database.Users.GetByUsernameAsync("missing", Token));

        await using var connection = await database.Connections.OpenAsync();
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT PasswordHash FROM Users WHERE Id = @id;";
        command.Parameters.AddWithValue("@id", saved.Id);
        Assert.Equal(HashFixture, await command.ExecuteScalarAsync());
    }

    [Fact]
    public async Task Duplicate_username_is_rejected_by_sqlite()
    {
        await database.Initializer.InitializeAsync();
        var original = await AddUser();
        await Assert.ThrowsAsync<DuplicateUsernameException>(() => AddUser());
        Assert.Equal(original.Id, (await database.Users.GetByUsernameAsync("owner", Token))!.Id);
    }

    [Theory]
    [InlineData(null, TaskStatus.Pending)]
    [InlineData("Notes ' and @parameters", TaskStatus.InProgress)]
    [InlineData(null, TaskStatus.Completed)]
    public async Task Task_insert_round_trips_fields_and_generated_id_under_another_culture(
        string? description, TaskStatus status)
    {
        await database.Initializer.InitializeAsync();
        var user = await AddUser();
        var input = NewTask(user.Id, description, status);
        var previous = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("ar-SA");
            var saved = await database.Tasks.AddAsync(input, Token);
            var second = await database.Tasks.AddAsync(input, Token);
            Assert.Equal(0, input.Id);
            Assert.True(saved.Id > 0);
            Assert.True(second.Id > saved.Id);
            Assert.NotSame(input, saved);
            var read = await database.Tasks.GetByIdAsync(saved.Id, user.Id, Token);
            Assert.Equal(input.Title, read!.Title);
            Assert.Equal(description, read.Description);
            Assert.Equal(status, read.Status);
            Assert.Equal(input.DueDate, read.DueDate);
            Assert.Equal(user.Id, read.UserId);

            await using var connection = await database.Connections.OpenAsync();
            using var command = connection.CreateCommand();
            command.CommandText = "SELECT DueDate, Status, Description FROM Tasks WHERE Id = @id AND UserId = @userId;";
            command.Parameters.AddWithValue("@id", saved.Id);
            command.Parameters.AddWithValue("@userId", user.Id);
            await using var reader = await command.ExecuteReaderAsync();
            Assert.True(await reader.ReadAsync());
            Assert.Equal("2028-02-29", reader.GetString(0));
            Assert.Equal((int)status, reader.GetInt32(1));
            Assert.Equal(description is null, reader.IsDBNull(2));
        }
        finally { CultureInfo.CurrentCulture = previous; }
    }

    [Fact]
    public async Task Reads_are_scoped_to_owner_and_missing_results_are_empty()
    {
        await database.Initializer.InitializeAsync();
        var owner = await AddUser();
        var other = await AddUser("other");
        var own = await database.Tasks.AddAsync(NewTask(owner.Id), Token);
        await database.Tasks.AddAsync(NewTask(other.Id), Token);
        Assert.Equal(own.Id, Assert.Single(await database.Tasks.GetByUserIdAsync(owner.Id, Token)).Id);
        Assert.Empty(await database.Tasks.GetByUserIdAsync(999, Token));
        Assert.Null(await database.Tasks.GetByIdAsync(own.Id, other.Id, Token));
        Assert.Null(await database.Tasks.GetByIdAsync(999, owner.Id, Token));
    }

    [Fact]
    public async Task Update_requires_owner_and_reports_missing_rows_without_mutating_other_tasks()
    {
        await database.Initializer.InitializeAsync();
        var owner = await AddUser();
        var other = await AddUser("other");
        var saved = await database.Tasks.AddAsync(NewTask(owner.Id, "original"), Token);
        var otherTask = await database.Tasks.AddAsync(NewTask(other.Id), Token);
        Assert.False(await database.Tasks.UpdateAsync(new(saved.Id, other.Id, "attack", null, saved.DueDate), Token));
        Assert.Equal("original", (await database.Tasks.GetByIdAsync(saved.Id, owner.Id, Token))!.Description);
        Assert.False(await database.Tasks.UpdateAsync(new(999, owner.Id, "missing", null, saved.DueDate), Token));
        var updated = new TaskItem(saved.Id, owner.Id, "Changed", null, new DateOnly(2000, 1, 1), TaskStatus.Completed);
        Assert.True(await database.Tasks.UpdateAsync(updated, Token));
        var read = await database.Tasks.GetByIdAsync(saved.Id, owner.Id, Token);
        Assert.Equal(updated.Title, read!.Title);
        Assert.Null(read.Description);
        Assert.Equal(updated.DueDate, read.DueDate);
        Assert.Equal(updated.Status, read.Status);
        Assert.Equal(otherTask.Title, (await database.Tasks.GetByIdAsync(otherTask.Id, other.Id, Token))!.Title);
        Assert.True(await database.Tasks.UpdateAsync(new(saved.Id, owner.Id, "Again", "restored", saved.DueDate), Token));
        Assert.Equal("restored", (await database.Tasks.GetByIdAsync(saved.Id, owner.Id, Token))!.Description);
    }

    [Fact]
    public async Task Delete_requires_owner_and_reports_missing_rows()
    {
        await database.Initializer.InitializeAsync();
        var owner = await AddUser();
        var other = await AddUser("other");
        var own = await database.Tasks.AddAsync(NewTask(owner.Id), Token);
        var otherTask = await database.Tasks.AddAsync(NewTask(other.Id), Token);
        Assert.False(await database.Tasks.DeleteAsync(own.Id, other.Id, Token));
        Assert.NotNull(await database.Tasks.GetByIdAsync(own.Id, owner.Id, Token));
        Assert.False(await database.Tasks.DeleteAsync(999, owner.Id, Token));
        Assert.True(await database.Tasks.DeleteAsync(own.Id, owner.Id, Token));
        Assert.False(await database.Tasks.DeleteAsync(own.Id, owner.Id, Token));
        Assert.Null(await database.Tasks.GetByIdAsync(own.Id, owner.Id, Token));
        Assert.NotNull(await database.Tasks.GetByIdAsync(otherTask.Id, other.Id, Token));
    }

    [Fact]
    public async Task Foreign_keys_are_enforced_on_repository_connections_even_when_config_disables_them()
    {
        await database.Initializer.InitializeAsync();
        var error = await Assert.ThrowsAsync<SqliteException>(() => database.Tasks.AddAsync(NewTask(999), Token));
        Assert.Equal(787, error.SqliteExtendedErrorCode);
        await using var connection = await database.Connections.OpenAsync();
        using var command = connection.CreateCommand();
        command.CommandText = "PRAGMA foreign_keys;";
        Assert.Equal(1L, await command.ExecuteScalarAsync());
    }

    [Fact]
    public async Task Schema_initialization_is_repeatable_and_preserves_records()
    {
        await database.Initializer.InitializeAsync();
        var user = await AddUser();
        var task = await database.Tasks.AddAsync(NewTask(user.Id), Token);
        await database.Initializer.InitializeAsync();
        Assert.Equal(user.Id, (await database.Users.GetByUsernameAsync(user.Username, Token))!.Id);
        Assert.Equal(task.Id, Assert.Single(await database.Tasks.GetByUserIdAsync(user.Id, Token)).Id);
    }

    [Fact]
    public async Task Seed_is_repeatable_and_does_not_reset_hash_or_recreate_deleted_demo_tasks()
    {
        await database.Initializer.InitializeAsync();
        var seeder = new SqliteDemoSeeder(database.Connections);
        await seeder.SeedAsync(HashFixture);
        var demo = await database.Users.GetByUsernameAsync("demo", Token);
        Assert.NotNull(demo);
        var tasks = await database.Tasks.GetByUserIdAsync(demo.Id, Token);
        Assert.Equal(3, tasks.Count);
        Assert.Equal(3, tasks.Select(t => t.Status).Distinct().Count());
        await database.Initializer.InitializeAsync();
        await seeder.SeedAsync("different-opaque-fixture");
        Assert.Equal(HashFixture, (await database.Users.GetByIdAsync(demo.Id, Token))!.PasswordHash);
        Assert.Equal(tasks.Select(t => t.Id).Order(), (await database.Tasks.GetByUserIdAsync(demo.Id, Token)).Select(t => t.Id).Order());
        await database.Tasks.DeleteAsync(tasks[0].Id, demo.Id, Token);
        await seeder.SeedAsync(HashFixture);
        Assert.Equal(2, (await database.Tasks.GetByUserIdAsync(demo.Id, Token)).Count);
        await using var connection = await database.Connections.OpenAsync();
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(Id) FROM Users;";
        Assert.Equal(1L, await command.ExecuteScalarAsync());
    }

    [Fact]
    public async Task Seed_does_not_add_demo_data_to_an_existing_user_named_demo()
    {
        await database.Initializer.InitializeAsync();
        var demo = await AddUser("demo");
        await new SqliteDemoSeeder(database.Connections).SeedAsync("different-opaque-fixture");
        Assert.Empty(await database.Tasks.GetByUserIdAsync(demo.Id, Token));
        Assert.Equal(HashFixture, (await database.Users.GetByIdAsync(demo.Id, Token))!.PasswordHash);
    }

    [Theory]
    [InlineData("initialize")]
    [InlineData("seed")]
    [InlineData("user-add")]
    [InlineData("user-id")]
    [InlineData("user-name")]
    [InlineData("task-add")]
    [InlineData("task-get")]
    [InlineData("task-list")]
    [InlineData("task-update")]
    [InlineData("task-delete")]
    public async Task Cancelled_operations_do_not_write(string operation)
    {
        await database.Initializer.InitializeAsync();
        var user = await AddUser();
        var task = await database.Tasks.AddAsync(NewTask(user.Id), Token);
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        var token = cancellation.Token;
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => operation switch
        {
            "initialize" => database.Initializer.InitializeAsync(token),
            "seed" => new SqliteDemoSeeder(database.Connections).SeedAsync(HashFixture, token),
            "user-add" => database.Users.AddAsync(new(0, "cancelled", HashFixture), token),
            "user-id" => database.Users.GetByIdAsync(user.Id, token),
            "user-name" => database.Users.GetByUsernameAsync(user.Username, token),
            "task-add" => database.Tasks.AddAsync(NewTask(user.Id), token),
            "task-get" => database.Tasks.GetByIdAsync(task.Id, user.Id, token),
            "task-list" => database.Tasks.GetByUserIdAsync(user.Id, token),
            "task-update" => database.Tasks.UpdateAsync(new(task.Id, user.Id, "changed", null, task.DueDate), token),
            "task-delete" => database.Tasks.DeleteAsync(task.Id, user.Id, token),
            _ => throw new ArgumentException(nameof(operation))
        });
        Assert.Single(await database.Tasks.GetByUserIdAsync(user.Id, Token));
        Assert.Equal(task.Title, (await database.Tasks.GetByIdAsync(task.Id, user.Id, Token))!.Title);
        Assert.Null(await database.Users.GetByUsernameAsync("cancelled", Token));
        Assert.Null(await database.Users.GetByUsernameAsync("demo", Token));
    }

    public void Dispose() => database.Dispose();

    [Fact]
    public async Task Seed_failure_rolls_back_user_and_all_tasks_and_can_be_retried()
    {
        await database.Initializer.InitializeAsync();
        await using (var connection = await database.Connections.OpenAsync())
        {
            using var command = connection.CreateCommand();
            // Fail after the first task to exercise rollback of partially executed seed writes.
            command.CommandText = """
                CREATE TRIGGER RejectSeedTask BEFORE INSERT ON Tasks
                WHEN NEW.Status = 1
                BEGIN SELECT RAISE(ABORT, 'Simulated seed failure'); END;
                """;
            await command.ExecuteNonQueryAsync();
        }
        var seeder = new SqliteDemoSeeder(database.Connections);
        await Assert.ThrowsAsync<SqliteException>(() => seeder.SeedAsync(HashFixture));
        Assert.Null(await database.Users.GetByUsernameAsync("demo", Token));
        await using (var connection = await database.Connections.OpenAsync())
        {
            using var command = connection.CreateCommand();
            command.CommandText = "SELECT COUNT(Id) FROM Tasks;";
            Assert.Equal(0L, await command.ExecuteScalarAsync());
            command.CommandText = "DROP TRIGGER RejectSeedTask;";
            await command.ExecuteNonQueryAsync();
        }
        await seeder.SeedAsync(HashFixture);
        var demo = await database.Users.GetByUsernameAsync("demo", Token);
        Assert.Equal(3, (await database.Tasks.GetByUserIdAsync(demo!.Id, Token)).Count);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public async Task Seed_requires_a_supplied_hash_and_never_creates_a_placeholder_user(string? hash)
    {
        await database.Initializer.InitializeAsync();
        await Assert.ThrowsAnyAsync<ArgumentException>(() => new SqliteDemoSeeder(database.Connections).SeedAsync(hash!));
        Assert.Null(await database.Users.GetByUsernameAsync("demo", Token));
    }
}
