using TaskStatus = TaskManager.Domain.TaskStatus;

namespace TaskManager.Application;

public sealed record TaskInput(string Title, string? Description, DateOnly DueDate,
    TaskStatus Status = TaskStatus.Pending);
