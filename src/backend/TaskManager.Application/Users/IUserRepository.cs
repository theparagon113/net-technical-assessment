using TaskManager.Domain;

namespace TaskManager.Application.Users;

public interface IUserRepository
{
    // Caller supplies an already hashed password; storage never hashes or verifies it.
    // Duplicate identity (including insert races) throws DuplicateUsernameException.
    Task<User> AddAsync(User user, CancellationToken cancellationToken);
    Task<User?> GetByIdAsync(int userId, CancellationToken cancellationToken);
    // Identity uses UsernamePolicy.Compare; returned Username retains display casing.
    Task<User?> GetByUsernameAsync(string username, CancellationToken cancellationToken);
}
