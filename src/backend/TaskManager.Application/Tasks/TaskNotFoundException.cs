namespace TaskManager.Application.Tasks;

public sealed class TaskNotFoundException : Exception
{
    public TaskNotFoundException() : base("Task was not found.") { }
}
