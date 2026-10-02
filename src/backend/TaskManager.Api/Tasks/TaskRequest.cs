using System.ComponentModel.DataAnnotations;
using TaskManager.Application.Tasks;
using TaskStatus = TaskManager.Domain.TaskStatus;

namespace TaskManager.Api.Tasks;

public sealed record TaskRequest(string Title, string? Description, [Required] DateOnly? DueDate,
    TaskStatus Status = TaskStatus.Pending)
{
    public TaskInput ToInput() => new(Title, Description, DueDate!.Value, Status);
}
