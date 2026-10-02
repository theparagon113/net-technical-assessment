using System.Net;
using System.Net.Http.Json;
using Microsoft.Data.Sqlite;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using TaskManager.SharedApiTests;
using TaskManager.Application.Authentication;
using TaskManager.Application.Tasks;
using TaskManager.Application.Users;
using TaskManager.Infrastructure.Persistence;
using TaskManager.Infrastructure.Persistence.Repositories;
using TaskProgram = TaskManager.Api.Program;
using AuthProgram = TaskManager.Auth.Api.Program;

namespace TaskManager.Api.Tests;

public sealed class StartupTests
{
    [Theory]
    [InlineData(false, null)]
    [InlineData(true, null)]
    [InlineData(false, "")]
    [InlineData(true, "")]
    [InlineData(false, "Data Source=")]
    [InlineData(true, "Data Source=")]
    [InlineData(false, "Data Source=taskmanager.db")]
    [InlineData(true, "Data Source=taskmanager.db")]
    [InlineData(false, "Data Source=./data/tasks.db")]
    [InlineData(true, "Data Source=./data/tasks.db")]
    [InlineData(false, "Data Source=:memory:")]
    [InlineData(true, "Data Source=:memory:")]
    public void Both_hosts_reject_missing_or_non_absolute_database(bool auth, string? connectionString)
    {
        using var settings = new TestSettings();
        settings.Values["ConnectionStrings:TaskManager"] = connectionString;
        if (auth)
        {
            using var factory = new ApiTestFactory<AuthProgram>(settings);
            Assert.Throws<InvalidOperationException>(() => factory.Client());
        }
        else
        {
            using var factory = new ApiTestFactory<TaskProgram>(settings);
            Assert.Throws<InvalidOperationException>(() => factory.Client());
        }
        Assert.False(File.Exists(settings.DatabasePath));
    }

    [Theory]
    [InlineData("Jwt:Issuer", "")]
    [InlineData("Jwt:Audience", "")]
    [InlineData("Jwt:SigningKey", null)]
    [InlineData("Jwt:SigningKey", "invalid-base64")]
    [InlineData("Jwt:SigningKey", "YWJj")]
    [InlineData("Jwt:LifetimeMinutes", "0")]
    [InlineData("Jwt:LifetimeMinutes", "61")]
    public void Both_hosts_reject_invalid_JWT_configuration_before_creating_database(string key, string? value)
    {
        using var settings = new TestSettings();
        settings.Values[key] = value;
        using var auth = new ApiTestFactory<AuthProgram>(settings);
        using var tasks = new ApiTestFactory<TaskProgram>(settings);
        Assert.ThrowsAny<ArgumentException>(() => auth.Client());
        Assert.ThrowsAny<ArgumentException>(() => tasks.Client());
        Assert.False(File.Exists(settings.DatabasePath));
    }

    [Fact]
    public async Task Task_host_starts_first_without_seed_then_auth_seeds_same_file_despite_different_roots()
    {
        using var settings = new TestSettings();
        var taskRoot = settings.ContentRoot("tasks");
        var authRoot = settings.ContentRoot("auth");
        using var taskFactory = new ApiTestFactory<TaskProgram>(settings, taskRoot);
        using var taskClient = taskFactory.Client();
        Assert.Equal(taskRoot, taskFactory.Services.GetRequiredService<IWebHostEnvironment>().ContentRootPath);
        var connections = new SqliteConnectionFactory(settings.Values["ConnectionStrings:TaskManager"]!);
        var users = new SqliteUserRepository(connections);
        Assert.True(File.Exists(settings.DatabasePath));
        Assert.Null(await users.GetByUsernameAsync("demo", default));
        await using (var connection = await connections.OpenAsync())
        {
            using var command = connection.CreateCommand();
            command.CommandText = "SELECT (SELECT COUNT(*) FROM Users) + (SELECT COUNT(*) FROM Tasks);";
            Assert.Equal(0L, await command.ExecuteScalarAsync());
        }
        using var authFactory = new ApiTestFactory<AuthProgram>(settings, authRoot);
        using var authClient = authFactory.Client();
        Assert.Equal(authRoot, authFactory.Services.GetRequiredService<IWebHostEnvironment>().ContentRootPath);
        var demo = (await users.GetByUsernameAsync("demo", default))!;
        Assert.NotNull(demo);
        var tasks = new SqliteTaskRepository(connections);
        Assert.Equal(3, (await tasks.GetByUserIdAsync(demo.Id, default)).Count);
        var login = await authClient.PostAsJsonAsync("/api/auth/login", new AuthInput("demo", "Demo123!"));
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        var result = (await login.Content.ReadFromJsonAsync<AuthResult>())!;
        HttpTest.Bearer(taskClient, result.AccessToken);
        Assert.Equal(3, (await taskClient.GetFromJsonAsync<TaskResult[]>("/api/tasks"))!.Length);
        Assert.Empty(Directory.GetFiles(taskRoot, "*.db", SearchOption.AllDirectories));
        Assert.Empty(Directory.GetFiles(authRoot, "*.db", SearchOption.AllDirectories));
    }

    [Fact]
    public async Task Repeated_and_concurrent_startup_preserves_changed_credentials_and_demo_task_edits_deletions()
    {
        using var settings = new TestSettings();
        var connections = new SqliteConnectionFactory(settings.Values["ConnectionStrings:TaskManager"]!);
        int demoId;
        string changedHash;
        int editedId;
        using (var factory = new ApiTestFactory<AuthProgram>(settings))
        {
            using var client = factory.Client();
            using var scope = factory.Services.CreateScope();
            var users = scope.ServiceProvider.GetRequiredService<IUserRepository>();
            var demo = (await users.GetByUsernameAsync("demo", default))!;
            demoId = demo.Id;
            changedHash = scope.ServiceProvider.GetRequiredService<IPasswordHasher>().Hash("Changed123!");
            await using var connection = await connections.OpenAsync();
            using var command = connection.CreateCommand();
            command.CommandText = "UPDATE Users SET PasswordHash=@hash, Username='DEMO' WHERE Id=@id;";
            command.Parameters.AddWithValue("@hash", changedHash);
            command.Parameters.AddWithValue("@id", demoId);
            await command.ExecuteNonQueryAsync();
            var tasks = new SqliteTaskRepository(connections);
            var seeded = await tasks.GetByUserIdAsync(demoId, default);
            editedId = seeded[0].Id;
            await new TaskService(tasks).UpdateAsync(demoId, editedId, new TaskInput("edited", null, new DateOnly(2026, 9, 1)));
            await tasks.DeleteAsync(seeded[1].Id, demoId, default);
        }
        await Task.WhenAll(Enumerable.Range(0, 4).Select(index => Task.Run(() =>
        {
            if (index % 2 == 0)
            {
                using var factory = new ApiTestFactory<AuthProgram>(settings);
                using var client = factory.Client();
            }
            else
            {
                using var factory = new ApiTestFactory<TaskProgram>(settings);
                using var client = factory.Client();
            }
        })));
        var saved = (await new SqliteUserRepository(connections).GetByUsernameAsync("demo", default))!;
        Assert.Equal(demoId, saved.Id);
        Assert.Equal("DEMO", saved.Username);
        Assert.Equal(changedHash, saved.PasswordHash);
        var remaining = await new SqliteTaskRepository(connections).GetByUserIdAsync(demoId, default);
        Assert.Equal(2, remaining.Count);
        Assert.Equal("edited", remaining.Single(task => task.Id == editedId).Title);
        using var final = new ApiTestFactory<AuthProgram>(settings);
        using var auth = final.Client();
        Assert.Equal(HttpStatusCode.OK, (await auth.PostAsJsonAsync("/api/auth/login", new AuthInput("demo", "Changed123!"))).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await auth.PostAsJsonAsync("/api/auth/login", new AuthInput("demo", "Demo123!"))).StatusCode);
    }

    [Fact]
    public async Task Concurrent_hosts_can_initialize_a_new_shared_file()
    {
        using var settings = new TestSettings();
        await Task.WhenAll(Task.Run(() =>
        {
            using var factory = new ApiTestFactory<AuthProgram>(settings);
            using var client = factory.Client();
        }), Task.Run(() =>
        {
            using var factory = new ApiTestFactory<TaskProgram>(settings);
            using var client = factory.Client();
        }));
        var connections = new SqliteConnectionFactory(settings.Values["ConnectionStrings:TaskManager"]!);
        var demo = (await new SqliteUserRepository(connections).GetByUsernameAsync("demo", default))!;
        Assert.Equal(3, (await new SqliteTaskRepository(connections).GetByUserIdAsync(demo.Id, default)).Count);
    }
}
