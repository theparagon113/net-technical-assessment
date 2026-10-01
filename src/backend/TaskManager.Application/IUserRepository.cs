using TaskManager.Domain;

namespace TaskManager.Application;

public interface IUserRepository
{
    // Caller supplies an already hashed password; storage never hashes or verifies it.
    Task<User> AddAsync(User user, CancellationToken cancellationToken);
    Task<User?> GetByIdAsync(int userId, CancellationToken cancellationToken);
    Task<User?> GetByUsernameAsync(string username, CancellationToken cancellationToken);
}
