using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using TaskManager.SharedApiTests;
using TaskManager.Application.Authentication;
using AuthProgram = TaskManager.Auth.Api.Program;

namespace TaskManager.Auth.Api.Tests;

public sealed class AuthHttpTests
{
    [Fact]
    public async Task Registration_login_public_and_current_user_use_real_pipeline()
    {
        using var settings = new TestSettings();
        using var factory = new ApiTestFactory<AuthProgram>(settings);
        using var client = factory.Client();
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/auth/public")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/auth/me")).StatusCode);
        var result = await HttpTest.RegisterAndLogin(client, " Alice ");
        Assert.Equal("Alice", result.Username);
        Assert.True(result.ExpiresAt > DateTimeOffset.UtcNow);
        var duplicate = await client.PostAsJsonAsync("/api/auth/register", new AuthInput("aLiCe", "Test123!"));
        Assert.Equal(HttpStatusCode.Conflict, duplicate.StatusCode);
        HttpTest.Bearer(client, result.AccessToken);
        var me = await client.GetAsync("/api/auth/me");
        Assert.Equal(HttpStatusCode.OK, me.StatusCode);
        var json = await me.Content.ReadAsStringAsync();
        using var document = JsonDocument.Parse(json);
        Assert.Equal(result.UserId, document.RootElement.GetProperty("userId").GetInt32());
        Assert.Equal("Alice", document.RootElement.GetProperty("username").GetString());
        Assert.DoesNotContain("password", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("token", json, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync("/api/tasks")).StatusCode);
    }
    [Theory]
    [InlineData("/api/auth/register", "{}")]
    [InlineData("/api/auth/register", "null")]
    [InlineData("/api/auth/register", "{broken")]
    [InlineData("/api/auth/register", "{\"username\":\"ab\",\"password\":\"Test123!\"}")]
    [InlineData("/api/auth/login", "{\"username\":\"valid\",\"password\":\"short\"}")]
    [InlineData("/api/auth/login", "{}")]
    public async Task Invalid_input_returns_problem_details(string route, string body)
    {
        using var settings = new TestSettings();
        using var factory = new ApiTestFactory<AuthProgram>(settings);
        using var client = factory.Client();
        var response = await client.PostAsync(route, new StringContent(body, Encoding.UTF8, "application/json"));
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }
    [Fact]
    public async Task Unknown_user_and_wrong_password_have_identical_generic_failure()
    {
        using var settings = new TestSettings();
        using var factory = new ApiTestFactory<AuthProgram>(settings);
        using var client = factory.Client();
        await HttpTest.RegisterAndLogin(client, "alice");
        var unknown = await client.PostAsJsonAsync("/api/auth/login", new AuthInput("unknown", "Test123!"));
        var wrong = await client.PostAsJsonAsync("/api/auth/login", new AuthInput("alice", "Wrong123!"));
        Assert.Equal(HttpStatusCode.Unauthorized, unknown.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, wrong.StatusCode);
        using var left = JsonDocument.Parse(await unknown.Content.ReadAsStringAsync());
        using var right = JsonDocument.Parse(await wrong.Content.ReadAsStringAsync());
        Assert.Equal(left.RootElement.GetProperty("title").GetString(), right.RootElement.GetProperty("title").GetString());
        Assert.Equal("Invalid username or password.", left.RootElement.GetProperty("title").GetString());
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
    public async Task Invalid_tokens_are_rejected_without_security_details(string variant)
    {
        using var settings = new TestSettings();
        using var factory = new ApiTestFactory<AuthProgram>(settings);
        using var client = factory.Client();
        HttpTest.Bearer(client, InvalidTokens.Create(settings, variant));
        var response = await client.GetAsync("/api/auth/me");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.DoesNotContain("error_description", response.Headers.WwwAuthenticate.ToString());
        Assert.DoesNotContain(settings.Values["Jwt:SigningKey"]!, await response.Content.ReadAsStringAsync());
    }
}
