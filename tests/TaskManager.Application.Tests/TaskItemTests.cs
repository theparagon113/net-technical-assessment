using TaskManager.Domain;
using TaskStatus = TaskManager.Domain.TaskStatus;

namespace TaskManager.Application.Tests;

public sealed class TaskItemTests
{
    private static readonly DateOnly DueDate = new(2026, 10, 4);

    [Fact]
    public void Valid_task_normalizes_text_and_preserves_identity_and_date()
    {
        var task = new TaskItem(1, 2, "  Review  ", "  Notes  ", DueDate);

        Assert.Equal(1, task.Id);
        Assert.Equal(2, task.UserId);
        Assert.Equal("Review", task.Title);
        Assert.Equal("Notes", task.Description);
        Assert.Equal(TaskStatus.Pending, task.Status);
        Assert.Equal(DueDate, task.DueDate);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" \t\n ")]
    public void Missing_title_is_rejected(string? title)
    {
        Assert.Throws<ArgumentException>(() => new TaskItem(0, 1, title!, null, DueDate));
    }

    [Fact]
    public void Null_title_is_rejected()
    {
        Assert.Throws<ArgumentNullException>(() => new TaskItem(0, 1, null!, null, DueDate));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Invalid_owner_is_rejected(int userId)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new TaskItem(0, userId, "Task", null, DueDate));
    }

    [Fact]
    public void Negative_task_id_is_rejected()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new TaskItem(-1, 1, "Task", null, DueDate));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public void Empty_description_is_stored_as_null(string? description)
    {
        Assert.Null(new TaskItem(0, 1, "Task", description, DueDate).Description);
    }

    [Theory]
    [InlineData(TaskStatus.Pending)]
    [InlineData(TaskStatus.InProgress)]
    [InlineData(TaskStatus.Completed)]
    public void All_defined_statuses_are_allowed(TaskStatus status)
    {
        Assert.Equal(status, new TaskItem(0, 1, "Task", null, DueDate, status).Status);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(3)]
    public void Undefined_status_is_rejected(int status)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new TaskItem(0, 1, "Task", null, DueDate, (TaskStatus)status));
    }

    [Fact]
    public void Documented_text_limits_allow_boundary_and_reject_excess()
    {
        var task = new TaskItem(0, 1, " " + new string('t', 120) + " ", new string('d', 1000), DueDate);
        Assert.Equal(120, task.Title.Length);
        Assert.Equal(1000, task.Description!.Length);
        Assert.Throws<ArgumentException>(() => new TaskItem(0, 1, new string('t', 121), null, DueDate));
        Assert.Throws<ArgumentException>(() => new TaskItem(0, 1, "Task", new string('d', 1001), DueDate));
    }

    [Fact]
    public void Past_due_date_is_allowed()
    {
        var past = new DateOnly(2000, 1, 1);
        Assert.Equal(past, new TaskItem(0, 1, "Task", null, past).DueDate);
    }
}
