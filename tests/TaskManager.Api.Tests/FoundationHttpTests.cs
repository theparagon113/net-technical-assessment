using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using TaskManager.SharedApiTests;
using TaskManager.Application.Authentication;
using TaskManager.Application.Users;
using TaskManager.Application.Tasks;
using TaskManager.Domain;
using AuthProgram = TaskManager.Auth.Api.Program;
using TaskProgram = TaskManager.Api.Program;

namespace TaskManager.Api.Tests;

public sealed class FoundationHttpTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Cors_allows_only_configured_origin_in_both_hosts(bool authHost)
    {
        using var settings = new TestSettings();
        settings.Values["Cors:FrontendOrigin"] = "http://localhost:4300";
        using var auth = new ApiTestFactory<AuthProgram>(settings);
        using var tasks = new ApiTestFactory<TaskProgram>(settings);
        using var client = authHost ? auth.Client() : tasks.Client();
        foreach (var origin in new[] { "http://localhost:4300", "http://localhost:4200", "https://untrusted.example" })
        {
            using var request = new HttpRequestMessage(HttpMethod.Options, authHost ? "/api/auth/login" : "/api/tasks");
            request.Headers.Add("Origin", origin);
            request.Headers.Add("Access-Control-Request-Method", "POST");
            request.Headers.Add("Access-Control-Request-Headers", "authorization,content-type");
            var response = await client.SendAsync(request);
            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
            if (origin == "http://localhost:4300")
                Assert.Equal(origin, Assert.Single(response.Headers.GetValues("Access-Control-Allow-Origin")));
            else
                Assert.False(response.Headers.Contains("Access-Control-Allow-Origin"));
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Unexpected_repository_failure_returns_safe_500_problem_details(bool authHost)
    {
        using var settings = new TestSettings();
        using var authFactory = new ApiTestFactory<AuthProgram>(settings);
        using var taskFactory = new ApiTestFactory<TaskProgram>(settings);
        using var auth = authFactory.Client();
        var login = await HttpTest.RegisterAndLogin(auth, "alice");
        HttpResponseMessage response;
        if (authHost)
        {
            using var failing = authFactory.WithWebHostBuilder(builder => builder.ConfigureTestServices(services =>
                services.AddScoped<IUserRepository, FailingUsers>()));
            using var client = failing.CreateClient();
            response = await client.PostAsJsonAsync("/api/auth/login", new AuthInput("alice", "Test123!"));
        }
        else
        {
            using var failing = taskFactory.WithWebHostBuilder(builder => builder.ConfigureTestServices(services =>
                services.AddScoped<ITaskRepository, FailingTasks>()));
            using var client = failing.CreateClient();
            HttpTest.Bearer(client, login.AccessToken);
            response = await client.GetAsync("/api/tasks");
        }
        using (response)
        {
            Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
            Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
            var body = await response.Content.ReadAsStringAsync();
            using var json = JsonDocument.Parse(body);
            Assert.Equal("An unexpected server error occurred.", json.RootElement.GetProperty("title").GetString());
            Assert.DoesNotContain("SELECT", body);
            Assert.DoesNotContain("sensitive", body);
            Assert.DoesNotContain("stack", body, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain(settings.Values["Jwt:SigningKey"]!, body);
        }
    }

    private sealed class FailingUsers : IUserRepository
    {
        public Task<User> AddAsync(User user, CancellationToken cancellationToken) => throw new InvalidOperationException("sensitive SQL SELECT");
        public Task<User?> GetByIdAsync(int userId, CancellationToken cancellationToken) => throw new InvalidOperationException("sensitive SQL SELECT");
        public Task<User?> GetByUsernameAsync(string username, CancellationToken cancellationToken) => throw new InvalidOperationException("sensitive SQL SELECT");
    }
    private sealed class FailingTasks : ITaskRepository
    {
        public Task<TaskItem> AddAsync(TaskItem task, CancellationToken cancellationToken) => throw new InvalidOperationException("sensitive SQL SELECT");
        public Task<TaskItem?> GetByIdAsync(int taskId, int userId, CancellationToken cancellationToken) => throw new InvalidOperationException("sensitive SQL SELECT");
        public Task<IReadOnlyList<TaskItem>> GetByUserIdAsync(int userId, CancellationToken cancellationToken) => throw new InvalidOperationException("sensitive SQL SELECT");
        public Task<bool> UpdateAsync(TaskItem task, CancellationToken cancellationToken) => throw new InvalidOperationException("sensitive SQL SELECT");
        public Task<bool> DeleteAsync(int taskId, int userId, CancellationToken cancellationToken) => throw new InvalidOperationException("sensitive SQL SELECT");
    }
}
