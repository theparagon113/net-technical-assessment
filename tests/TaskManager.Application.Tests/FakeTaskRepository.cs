using TaskManager.Application;
using TaskManager.Domain;

namespace TaskManager.Application.Tests;

internal sealed class FakeTaskRepository : ITaskRepository
{
    private readonly Dictionary<int, TaskItem> tasks = [];
    private int nextId = 1;

    public Task<TaskItem> AddAsync(TaskItem task, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var saved = new TaskItem(nextId++, task.UserId, task.Title, task.Description, task.DueDate, task.Status);
        tasks.Add(saved.Id, saved);
        return Task.FromResult(saved);
    }

    public Task<TaskItem?> GetByIdAsync(int taskId, int userId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        tasks.TryGetValue(taskId, out var task);
        return Task.FromResult(task?.UserId == userId ? task : null);
    }

    public Task<IReadOnlyList<TaskItem>> GetByUserIdAsync(int userId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult<IReadOnlyList<TaskItem>>(tasks.Values.Where(task => task.UserId == userId).ToArray());
    }

    public Task<bool> UpdateAsync(TaskItem task, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!tasks.TryGetValue(task.Id, out var existing) || existing.UserId != task.UserId)
            return Task.FromResult(false);

        tasks[task.Id] = task;
        return Task.FromResult(true);
    }

    public Task<bool> DeleteAsync(int taskId, int userId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(tasks.TryGetValue(taskId, out var task) && task.UserId == userId && tasks.Remove(taskId));
    }
}
