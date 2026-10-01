using TaskManager.Domain;
using TaskStatus = TaskManager.Domain.TaskStatus;

namespace TaskManager.Application;

public sealed record TaskResult(int Id, int UserId, string Title, string? Description,
    TaskStatus Status, DateOnly DueDate)
{
    internal static TaskResult FromTask(TaskItem task) =>
        new(task.Id, task.UserId, task.Title, task.Description, task.Status, task.DueDate);
}
