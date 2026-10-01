using TaskManager.Application;
using TaskStatus = TaskManager.Domain.TaskStatus;

namespace TaskManager.Application.Tests;

public sealed class TaskServiceTests
{
    private static readonly DateOnly DueDate = new(2026, 10, 4);
    private readonly TaskService service = new(new FakeTaskRepository());
    private static TaskInput Input(string title = "Task", TaskStatus status = TaskStatus.Pending) =>
        new(title, " Notes ", DueDate, status);

    [Fact]
    public async Task Create_assigns_current_user_and_persists_normalized_data()
    {
        var created = await service.CreateAsync(1, Input(" Task "));

        Assert.True(created.Id > 0);
        Assert.Equal(1, created.UserId);
        Assert.Equal("Task", created.Title);
        Assert.Equal("Notes", created.Description);
        Assert.Equal(TaskStatus.Pending, created.Status);
        Assert.Equal(DueDate, created.DueDate);
        Assert.Equal(created, await service.GetByIdAsync(1, created.Id));
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    public async Task Invalid_creation_does_not_persist(string title)
    {
        await Assert.ThrowsAsync<ArgumentException>(() => service.CreateAsync(1, Input(title)));
        Assert.Empty(await service.GetByUserIdAsync(1));
    }

    [Fact]
    public async Task Owner_can_retrieve_own_task()
    {
        var own = await service.CreateAsync(1, Input());
        await service.CreateAsync(2, Input("Other"));
        Assert.Equal(own, await service.GetByIdAsync(1, own.Id));
    }

    [Fact]
    public async Task Listing_returns_only_current_users_tasks()
    {
        var own = await service.CreateAsync(1, Input());
        await service.CreateAsync(2, Input("Other"));
        Assert.Equal(own, Assert.Single(await service.GetByUserIdAsync(1)));
        Assert.Empty(await service.GetByUserIdAsync(3));
    }

    [Theory]
    [InlineData("get")]
    [InlineData("update")]
    [InlineData("delete")]
    public async Task Cross_user_and_missing_operations_have_same_error_and_leave_task_unchanged(string operation)
    {
        var own = await service.CreateAsync(1, Input());
        var inaccessible = await Assert.ThrowsAsync<TaskNotFoundException>(() => Execute(operation, 2, own.Id));
        var missing = await Assert.ThrowsAsync<TaskNotFoundException>(() => Execute(operation, 2, 999));

        Assert.Equal(missing.Message, inaccessible.Message);
        Assert.Equal(own, await service.GetByIdAsync(1, own.Id));
    }

    [Fact]
    public async Task Update_changes_allowed_fields_and_preserves_identity_and_owner()
    {
        var created = await service.CreateAsync(1, Input());
        var date = new DateOnly(2000, 1, 1);
        var updated = await service.UpdateAsync(1, created.Id, new TaskInput(" Changed ", null, date, TaskStatus.Completed));

        Assert.Equal(created.Id, updated.Id);
        Assert.Equal(created.UserId, updated.UserId);
        Assert.Equal("Changed", updated.Title);
        Assert.Null(updated.Description);
        Assert.Equal(date, updated.DueDate);
        Assert.Equal(TaskStatus.Completed, updated.Status);
        Assert.Equal(updated, await service.GetByIdAsync(1, created.Id));
        Assert.Equal("Task", created.Title);
    }

    [Theory]
    [InlineData("", 0)]
    [InlineData("  ", 0)]
    [InlineData("Valid", 99)]
    public async Task Invalid_update_leaves_existing_task_unchanged(string title, int status)
    {
        var created = await service.CreateAsync(1, Input());
        await Assert.ThrowsAnyAsync<ArgumentException>(() => service.UpdateAsync(1, created.Id, Input(title, (TaskStatus)status)));
        Assert.Equal(created, await service.GetByIdAsync(1, created.Id));
    }

    [Fact]
    public async Task Owner_can_delete_task_without_affecting_another_users_task()
    {
        var own = await service.CreateAsync(1, Input());
        var other = await service.CreateAsync(2, Input());
        await service.DeleteAsync(1, own.Id);
        Assert.Empty(await service.GetByUserIdAsync(1));
        await Assert.ThrowsAsync<TaskNotFoundException>(() => service.GetByIdAsync(1, own.Id));
        Assert.Equal(other, await service.GetByIdAsync(2, other.Id));
    }

    [Theory]
    [InlineData("create")]
    [InlineData("get")]
    [InlineData("list")]
    [InlineData("update")]
    [InlineData("delete")]
    public async Task Invalid_current_user_is_rejected(string operation)
    {
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => Execute(operation, 0, 1));
    }

    [Theory]
    [InlineData("get")]
    [InlineData("update")]
    [InlineData("delete")]
    public async Task Invalid_task_identifier_is_rejected(string operation)
    {
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => Execute(operation, 1, 0));
    }

    [Theory]
    [InlineData("create")]
    [InlineData("update")]
    public async Task Null_input_is_rejected(string operation)
    {
        var own = await service.CreateAsync(1, Input());
        await Assert.ThrowsAsync<ArgumentNullException>(() => operation == "create"
            ? service.CreateAsync(1, null!)
            : service.UpdateAsync(1, own.Id, null!));
    }

    [Theory]
    [InlineData("create")]
    [InlineData("get")]
    [InlineData("list")]
    [InlineData("update")]
    [InlineData("delete")]
    public async Task Cancellation_is_forwarded_to_repository(string operation)
    {
        var own = await service.CreateAsync(1, Input());
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => Execute(operation, 1, own.Id, cancellation.Token));
        Assert.Equal(own, await service.GetByIdAsync(1, own.Id));
    }

    private Task Execute(string operation, int userId, int taskId, CancellationToken token = default) => operation switch
    {
        "create" => service.CreateAsync(userId, Input(), token),
        "get" => service.GetByIdAsync(userId, taskId, token),
        "list" => service.GetByUserIdAsync(userId, token),
        "update" => service.UpdateAsync(userId, taskId, Input("Changed"), token),
        "delete" => service.DeleteAsync(userId, taskId, token),
        _ => throw new ArgumentException("Unknown operation", nameof(operation))
    };
}
