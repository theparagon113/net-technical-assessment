using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using TaskManager.Application.Authentication;

namespace TaskManager.SharedApiTests;

internal sealed class ApiTestFactory<TProgram>(TestSettings settings, string? contentRoot = null)
    : WebApplicationFactory<TProgram> where TProgram : class
{
    protected override IHost CreateHost(IHostBuilder builder)
    {
        // Host configuration reaches CreateBuilder before fail-fast composition runs.
        builder.ConfigureHostConfiguration(config => config.AddInMemoryCollection(settings.Values));
        return base.CreateHost(builder);
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        if (contentRoot is not null) builder.UseContentRoot(contentRoot);
        builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(settings.Values));
    }
    public HttpClient Client() => CreateClient(new WebApplicationFactoryClientOptions
    {
        BaseAddress = new Uri("https://localhost"), AllowAutoRedirect = false
    });
}

internal sealed class TestSettings : IDisposable
{
    public string DirectoryPath { get; } = Path.Combine(Path.GetTempPath(), "task-manager-api-tests", Guid.NewGuid().ToString("N"));
    public string DatabasePath => Path.Combine(DirectoryPath, "data", "shared.db");
    public Dictionary<string, string?> Values { get; }
    public TestSettings()
    {
        Directory.CreateDirectory(DirectoryPath);
        Values = new()
        {
            ["ConnectionStrings:TaskManager"] = $"Data Source={DatabasePath};Pooling=False",
            ["Jwt:Issuer"] = "api-test-issuer", ["Jwt:Audience"] = "api-test-backend",
            ["Jwt:SigningKey"] = Convert.ToBase64String(RandomNumberGenerator.GetBytes(48)),
            ["Jwt:LifetimeMinutes"] = "15", ["Cors:FrontendOrigin"] = "http://localhost:4200"
        };
    }
    public string ContentRoot(string name)
    {
        var root = Path.Combine(DirectoryPath, name);
        Directory.CreateDirectory(root);
        return root;
    }
    public void Dispose() => Directory.Delete(DirectoryPath, recursive: true);
}

internal static class HttpTest
{
    public static async Task<AuthResult> RegisterAndLogin(HttpClient auth, string username)
    {
        var input = new AuthInput(username, "Test123!");
        using var registration = await auth.PostAsJsonAsync("/api/auth/register", input);
        Assert.Equal(System.Net.HttpStatusCode.Created, registration.StatusCode);
        using var login = await auth.PostAsJsonAsync("/api/auth/login", input);
        Assert.Equal(System.Net.HttpStatusCode.OK, login.StatusCode);
        return (await login.Content.ReadFromJsonAsync<AuthResult>())!;
    }
    public static void Bearer(HttpClient client, string token) =>
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
}
