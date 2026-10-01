using TaskManager.Domain;

namespace TaskManager.Application;

public interface ITaskRepository
{
    // Returns the saved task with its storage-assigned identifier.
    Task<TaskItem> AddAsync(TaskItem task, CancellationToken cancellationToken);

    // All reads and writes must be scoped to ownership, including within storage queries.
    Task<TaskItem?> GetByIdAsync(int taskId, int userId, CancellationToken cancellationToken);
    Task<IReadOnlyList<TaskItem>> GetByUserIdAsync(int userId, CancellationToken cancellationToken);

    // Match both Id and UserId; false means the owned task no longer exists.
    Task<bool> UpdateAsync(TaskItem task, CancellationToken cancellationToken);
    Task<bool> DeleteAsync(int taskId, int userId, CancellationToken cancellationToken);
}
