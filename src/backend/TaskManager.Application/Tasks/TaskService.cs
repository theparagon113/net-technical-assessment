using TaskManager.Domain;

namespace TaskManager.Application.Tasks;

public sealed class TaskService
{
    private readonly ITaskRepository repository;

    public TaskService(ITaskRepository repository)
    {
        ArgumentNullException.ThrowIfNull(repository);
        this.repository = repository;
    }

    public async Task<TaskResult> CreateAsync(int userId, TaskInput input, CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(userId);
        ArgumentNullException.ThrowIfNull(input);
        var task = new TaskItem(0, userId, input.Title, input.Description, input.DueDate, input.Status);
        var saved = await repository.AddAsync(task, cancellationToken);
        return TaskResult.FromTask(saved);
    }

    public async Task<TaskResult> GetByIdAsync(int userId, int taskId, CancellationToken cancellationToken = default)
    {
        var task = await FindOwnedTaskAsync(userId, taskId, cancellationToken);
        return TaskResult.FromTask(task);
    }

    public async Task<IReadOnlyList<TaskResult>> GetByUserIdAsync(int userId, CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(userId);
        var tasks = await repository.GetByUserIdAsync(userId, cancellationToken);
        return tasks.Where(task => task.UserId == userId).Select(TaskResult.FromTask).ToArray();
    }

    public async Task<TaskResult> UpdateAsync(int userId, int taskId, TaskInput input,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        var existing = await FindOwnedTaskAsync(userId, taskId, cancellationToken);
        var updated = new TaskItem(existing.Id, existing.UserId, input.Title, input.Description, input.DueDate, input.Status);

        if (!await repository.UpdateAsync(updated, cancellationToken))
            throw new TaskNotFoundException();

        return TaskResult.FromTask(updated);
    }

    public async Task DeleteAsync(int userId, int taskId, CancellationToken cancellationToken = default)
    {
        await FindOwnedTaskAsync(userId, taskId, cancellationToken);
        if (!await repository.DeleteAsync(taskId, userId, cancellationToken))
            throw new TaskNotFoundException();
    }

    private async Task<TaskItem> FindOwnedTaskAsync(int userId, int taskId, CancellationToken cancellationToken)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(userId);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(taskId);
        var task = await repository.GetByIdAsync(taskId, userId, cancellationToken);
        if (task is null || task.UserId != userId)
            throw new TaskNotFoundException();

        return task;
    }
}
