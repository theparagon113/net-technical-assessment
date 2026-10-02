using System.Net;
using System.Net.Http.Json;
using System.Text;
using TaskManager.SharedApiTests;
using TaskManager.Application.Tasks;
using AuthProgram = TaskManager.Auth.Api.Program;
using TaskProgram = TaskManager.Api.Program;

namespace TaskManager.Api.Tests;

public sealed class TaskHttpTests
{
    [Fact]
    public async Task Auth_login_token_authenticates_task_CRUD_and_enforces_cross_user_isolation()
    {
        using var settings = new TestSettings();
        using var taskFactory = new ApiTestFactory<TaskProgram>(settings, settings.ContentRoot("task-root"));
        using var authFactory = new ApiTestFactory<AuthProgram>(settings, settings.ContentRoot("auth-root"));
        using var alice = taskFactory.Client();
        using var bob = taskFactory.Client();
        using var auth = authFactory.Client();
        var first = await HttpTest.RegisterAndLogin(auth, "alice");
        var second = await HttpTest.RegisterAndLogin(auth, "bob");
        HttpTest.Bearer(alice, first.AccessToken);
        HttpTest.Bearer(bob, second.AccessToken);
        Assert.Empty((await alice.GetFromJsonAsync<TaskResult[]>("/api/tasks"))!);
        var input = new { title = " Alice task ", description = "notes", dueDate = "2026-10-03", status = 1, userId = second.UserId };
        var created = await alice.PostAsJsonAsync("/api/tasks?userId=" + second.UserId, input);
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var task = (await created.Content.ReadFromJsonAsync<TaskResult>())!;
        Assert.Equal(first.UserId, task.UserId);
        Assert.Equal("Alice task", task.Title);
        Assert.Equal($"https://localhost/api/tasks/{task.Id}", created.Headers.Location!.ToString());
        Assert.Equal(task, await alice.GetFromJsonAsync<TaskResult>(created.Headers.Location));
        Assert.Single((await alice.GetFromJsonAsync<TaskResult[]>("/api/tasks"))!);
        Assert.Empty((await bob.GetFromJsonAsync<TaskResult[]>("/api/tasks"))!);
        Assert.Equal(HttpStatusCode.NotFound, (await bob.GetAsync($"/api/tasks/{task.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await bob.PutAsJsonAsync($"/api/tasks/{task.Id}", input)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await bob.DeleteAsync($"/api/tasks/{task.Id}")).StatusCode);
        var update = new { title = "Updated", description = (string?)null, dueDate = "2026-09-01", status = 2 };
        var updated = await alice.PutAsJsonAsync($"/api/tasks/{task.Id}", update);
        Assert.Equal(HttpStatusCode.OK, updated.StatusCode);
        var representation = (await updated.Content.ReadFromJsonAsync<TaskResult>())!;
        Assert.Equal("Updated", representation.Title);
        Assert.Equal(first.UserId, representation.UserId);
        Assert.Equal(TaskManager.Domain.TaskStatus.Completed, representation.Status);
        Assert.Equal(representation, await alice.GetFromJsonAsync<TaskResult>($"/api/tasks/{task.Id}"));
        Assert.Equal(HttpStatusCode.NoContent, (await alice.DeleteAsync($"/api/tasks/{task.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await alice.GetAsync($"/api/tasks/{task.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await alice.PutAsJsonAsync($"/api/tasks/{task.Id}", update)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await alice.DeleteAsync($"/api/tasks/{task.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await alice.GetAsync("/api/auth/public")).StatusCode);
    }

    [Theory]
    [InlineData("GET", "/api/tasks")]
    [InlineData("GET", "/api/tasks/1")]
    [InlineData("POST", "/api/tasks")]
    [InlineData("PUT", "/api/tasks/1")]
    [InlineData("DELETE", "/api/tasks/1")]
    public async Task All_task_verbs_require_authentication(string method, string route)
    {
        using var settings = new TestSettings();
        using var factory = new ApiTestFactory<TaskProgram>(settings);
        using var client = factory.Client();
        using var request = new HttpRequestMessage(new HttpMethod(method), route);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.SendAsync(request)).StatusCode);
    }

    [Theory]
    [InlineData("malformed")]
    [InlineData("signature")]
    [InlineData("issuer")]
    [InlineData("audience")]
    [InlineData("expired")]
    [InlineData("future")]
    [InlineData("missing-exp")]
    [InlineData("missing-sub")]
    [InlineData("zero-sub")]
    [InlineData("negative-sub")]
    [InlineData("invalid-sub")]
    [InlineData("overflow-sub")]
    [InlineData("duplicate-sub")]
    [InlineData("unsigned")]
    [InlineData("algorithm")]
    public async Task Invalid_tokens_are_rejected_by_task_middleware(string variant)
    {
        using var settings = new TestSettings();
        using var factory = new ApiTestFactory<TaskProgram>(settings);
        using var client = factory.Client();
        HttpTest.Bearer(client, InvalidTokens.Create(settings, variant));
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/tasks")).StatusCode);
    }

    [Theory]
    [InlineData("{}")]
    [InlineData("null")]
    [InlineData("{broken")]
    [InlineData("{\"title\":\"valid\"}")]
    [InlineData("{\"title\":\"valid\",\"dueDate\":null}")]
    [InlineData("{\"title\":\"valid\",\"dueDate\":\"not-a-date\"}")]
    [InlineData("{\"title\":\" \",\"dueDate\":\"2026-10-03\"}")]
    [InlineData("{\"title\":\"valid\",\"dueDate\":\"2026-10-03\",\"status\":99}")]
    public async Task Invalid_task_input_returns_400_without_mutation(string body)
    {
        using var settings = new TestSettings();
        using var authFactory = new ApiTestFactory<AuthProgram>(settings);
        using var taskFactory = new ApiTestFactory<TaskProgram>(settings);
        using var auth = authFactory.Client();
        using var tasks = taskFactory.Client();
        HttpTest.Bearer(tasks, (await HttpTest.RegisterAndLogin(auth, "alice")).AccessToken);
        var created = await tasks.PostAsJsonAsync("/api/tasks", new { title = "original", dueDate = "2026-10-03" });
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        foreach (var method in new[] { HttpMethod.Post, HttpMethod.Put })
        {
            using var request = new HttpRequestMessage(method, method == HttpMethod.Post ? new Uri("/api/tasks", UriKind.Relative) : created.Headers.Location);
            request.Content = new StringContent(body, Encoding.UTF8, "application/json");
            var response = await tasks.SendAsync(request);
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        }
        var saved = (await tasks.GetFromJsonAsync<TaskResult>(created.Headers.Location))!;
        Assert.Equal("original", saved.Title);
        Assert.Single((await tasks.GetFromJsonAsync<TaskResult[]>("/api/tasks"))!);
        Assert.Equal(HttpStatusCode.BadRequest, (await tasks.GetAsync("/api/tasks/not-an-integer")).StatusCode);
    }
}
