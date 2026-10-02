using TaskManager.Application.Authentication;
using TaskManager.Application.Users;
using TaskManager.Domain;

namespace TaskManager.Application.Tests;

public sealed class AuthServiceTests
{
    private readonly Users users = new();
    private readonly Passwords passwords = new();
    private readonly Tokens tokens = new();
    private AuthService Service => new(users, passwords, tokens);

    [Fact]
    public void Input_diagnostic_string_does_not_expose_password()
    {
        var input = new AuthInput("Daniel", "secret-password!");
        Assert.DoesNotContain(input.Password, input.ToString());
        var token = new AccessToken("sensitive-token", DateTimeOffset.UtcNow);
        Assert.DoesNotContain(token.Value, token.ToString());
        var result = new AuthResult(1, "Daniel", token.Value, token.ExpiresAt);
        Assert.DoesNotContain(token.Value, result.ToString());
    }

    [Fact]
    public async Task Registration_persists_hash_and_returns_safe_trimmed_display_identity()
    {
        var result = await Service.RegisterAsync(new(" Daniel ", "password!"));
        Assert.Equal("Daniel", result.Username);
        Assert.Equal(1, result.UserId);
        Assert.Equal("token", result.AccessToken);
        Assert.Equal("hashed:password!", users.Saved!.PasswordHash);
        Assert.NotEqual("password!", users.Saved.PasswordHash);
        Assert.Equal(1, tokens.Calls);
    }

    [Theory]
    [InlineData("daniel")]
    [InlineData("DANIEL")]
    [InlineData(" DaNiEl ")]
    public async Task All_casings_share_login_and_duplicate_identity(string username)
    {
        await Service.RegisterAsync(new("Daniel", "password!"));
        var result = await Service.LoginAsync(new(username, "password!"));
        Assert.Equal("Daniel", result.Username);
        await Assert.ThrowsAsync<DuplicateUsernameException>(() => Service.RegisterAsync(new(username, "password!")));
        Assert.Equal(2, tokens.Calls);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("ab")]
    [InlineData("bad\nname")]
    public async Task Invalid_username_rejected_before_dependencies(string? name)
    {
        await Assert.ThrowsAnyAsync<ArgumentException>(() => Service.RegisterAsync(new(name!, "password!")));
        await Assert.ThrowsAnyAsync<ArgumentException>(() => Service.LoginAsync(new(name!, "password!")));
        Assert.Equal(0, users.Lookups);
        Assert.Equal(0, tokens.Calls);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("        ")]
    [InlineData("short")]
    public async Task Invalid_password_rejected_before_dependencies(string? password)
    {
        await Assert.ThrowsAnyAsync<ArgumentException>(() => Service.RegisterAsync(new("Daniel", password!)));
        await Assert.ThrowsAnyAsync<ArgumentException>(() => Service.LoginAsync(new("Daniel", password!)));
        Assert.Equal(0, users.Lookups);
        Assert.Equal(0, tokens.Calls);
    }

    [Theory]
    [InlineData(3, 8)]
    [InlineData(64, 128)]
    public async Task Inclusive_length_boundaries(int usernameLength, int passwordLength) =>
        await Service.RegisterAsync(new(new string('a', usernameLength), new string('p', passwordLength)));

    [Fact]
    public async Task Excessive_lengths_rejected()
    {
        await Assert.ThrowsAsync<ArgumentException>(() => Service.RegisterAsync(new(new string('a', 65), "password!")));
        await Assert.ThrowsAsync<ArgumentException>(() => Service.RegisterAsync(new("Daniel", new string('p', 129))));
    }

    [Fact]
    public async Task Login_failures_are_identical_and_never_issue_tokens()
    {
        var missing = await Assert.ThrowsAsync<InvalidCredentialsException>(() => Service.LoginAsync(new("Daniel", "password!")));
        users.Saved = new(1, "Daniel", "hashed:password!");
        var wrong = await Assert.ThrowsAsync<InvalidCredentialsException>(() => Service.LoginAsync(new("Daniel", "incorrect")));
        Assert.Equal(missing.Message, wrong.Message);
        Assert.Equal(0, tokens.Calls);
    }

    [Fact]
    public async Task Insert_race_returns_duplicate_failure_without_token()
    {
        users.Race = true;
        await Assert.ThrowsAsync<DuplicateUsernameException>(() => Service.RegisterAsync(new("Daniel", "password!")));
        Assert.Equal(0, tokens.Calls);
    }

    [Fact]
    public async Task Cancellation_propagates_to_all_repository_operations()
    {
        using var source = new CancellationTokenSource();
        await Service.RegisterAsync(new("Daniel", "password!"), source.Token);
        await Service.LoginAsync(new("Daniel", "password!"), source.Token);
        Assert.All(users.SeenTokens, token => Assert.Equal(source.Token, token));
        source.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => Service.RegisterAsync(new("second", "password!"), source.Token));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => Service.LoginAsync(new("Daniel", "password!"), source.Token));
        Assert.Equal(2, tokens.Calls);
    }

    private sealed class Users : IUserRepository
    {
        public User? Saved;
        public bool Race;
        public int Lookups;
        public List<CancellationToken> SeenTokens { get; } = [];
        public Task<User> AddAsync(User user, CancellationToken token)
        {
            SeenTokens.Add(token);
            if (Race) throw new DuplicateUsernameException();
            Saved = new(1, user.Username, user.PasswordHash);
            return Task.FromResult(Saved);
        }
        public Task<User?> GetByUsernameAsync(string username, CancellationToken token)
        {
            Lookups++;
            SeenTokens.Add(token);
            return Task.FromResult(Saved is not null && UsernamePolicy.Compare(Saved.Username, username) == 0 ? Saved : null);
        }
        public Task<User?> GetByIdAsync(int id, CancellationToken token) => throw new NotSupportedException();
    }
    private sealed class Passwords : IPasswordHasher
    {
        public string Hash(string password) => "hashed:" + password;
        public bool Verify(string password, string hash) => hash == Hash(password);
    }
    private sealed class Tokens : ITokenService
    {
        public int Calls;
        public AccessToken Create(User user)
        {
            Calls++;
            return new("token", DateTimeOffset.UtcNow.AddMinutes(15));
        }
    }
}
