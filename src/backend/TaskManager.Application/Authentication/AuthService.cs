using TaskManager.Application.Users;
using TaskManager.Domain;

namespace TaskManager.Application.Authentication;

public sealed class AuthService(IUserRepository users, IPasswordHasher passwords, ITokenService tokens)
{
    public async Task<AuthResult> RegisterAsync(AuthInput input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();
        var username = UsernamePolicy.ValidateAndTrim(input.Username);
        PasswordPolicy.Validate(input.Password);
        if (await users.GetByUsernameAsync(username, cancellationToken) is not null)
            throw new DuplicateUsernameException();
        var hash = passwords.Hash(input.Password);
        cancellationToken.ThrowIfCancellationRequested();
        // The repository also translates unique-constraint races into DuplicateUsernameException.
        var user = await users.AddAsync(new User(0, username, hash), cancellationToken);
        cancellationToken.ThrowIfCancellationRequested();
        return Authenticate(user);
    }

    public async Task<AuthResult> LoginAsync(AuthInput input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();
        var username = UsernamePolicy.ValidateAndTrim(input.Username);
        PasswordPolicy.Validate(input.Password);
        var user = await users.GetByUsernameAsync(username, cancellationToken);
        if (user is null || !passwords.Verify(input.Password, user.PasswordHash))
            throw new InvalidCredentialsException();
        cancellationToken.ThrowIfCancellationRequested();
        return Authenticate(user);
    }

    private AuthResult Authenticate(User user)
    {
        var token = tokens.Create(user);
        return new(user.Id, user.Username, token.Value, token.ExpiresAt);
    }
}
