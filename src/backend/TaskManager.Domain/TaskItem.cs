namespace TaskManager.Domain;

public sealed class TaskItem
{
    public int Id { get; }
    public int UserId { get; }
    public string Title { get; }
    public string? Description { get; }
    public TaskStatus Status { get; }
    public DateOnly DueDate { get; }

    // Zero represents a new task whose identifier has not yet been assigned by storage.
    public TaskItem(int id, int userId, string title, string? description, DateOnly dueDate,
        TaskStatus status = TaskStatus.Pending)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(id);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(userId);
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        title = title.Trim();
        description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();

        if (title.Length > 120)
            throw new ArgumentException("Title must be at most 120 characters.", nameof(title));
        if (description?.Length > 1000)
            throw new ArgumentException("Description must be at most 1000 characters.", nameof(description));
        if (!Enum.IsDefined(status))
            throw new ArgumentOutOfRangeException(nameof(status), "Task status is invalid.");

        Id = id;
        UserId = userId;
        Title = title;
        Description = description;
        Status = status;
        DueDate = dueDate;
    }
}
