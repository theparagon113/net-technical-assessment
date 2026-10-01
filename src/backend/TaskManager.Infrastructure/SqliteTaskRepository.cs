using TaskManager.Application;
using TaskManager.Domain;
using System.Globalization;
using Microsoft.Data.Sqlite;
using TaskStatus = TaskManager.Domain.TaskStatus;

namespace TaskManager.Infrastructure;

public sealed class SqliteTaskRepository(SqliteConnectionFactory connections) : ITaskRepository
{
    public async Task<TaskItem> AddAsync(TaskItem task, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(task);
        if (task.Id != 0)
            throw new ArgumentException("Only an unsaved task can be inserted.", nameof(task));
        await using var connection = await connections.OpenAsync(cancellationToken);
        using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO Tasks (UserId, Title, Description, Status, DueDate)
            VALUES (@userId, @title, @description, @status, @dueDate)
            RETURNING Id;
            """;
        AddTaskParameters(command, task);
        var id = checked((int)(long)(await command.ExecuteScalarAsync(cancellationToken))!);
        return new TaskItem(id, task.UserId, task.Title, task.Description, task.DueDate, task.Status);
    }

    public async Task<TaskItem?> GetByIdAsync(int taskId, int userId, CancellationToken cancellationToken)
    {
        await using var connection = await connections.OpenAsync(cancellationToken);
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT Id, UserId, Title, Description, Status, DueDate FROM Tasks
            WHERE Id = @id AND UserId = @userId;
            """;
        command.Parameters.AddWithValue("@id", taskId);
        command.Parameters.AddWithValue("@userId", userId);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        return await reader.ReadAsync(cancellationToken) ? Map(reader) : null;
    }

    public async Task<IReadOnlyList<TaskItem>> GetByUserIdAsync(int userId, CancellationToken cancellationToken)
    {
        await using var connection = await connections.OpenAsync(cancellationToken);
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT Id, UserId, Title, Description, Status, DueDate FROM Tasks
            WHERE UserId = @userId;
            """;
        command.Parameters.AddWithValue("@userId", userId);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        var tasks = new List<TaskItem>();
        while (await reader.ReadAsync(cancellationToken))
            tasks.Add(Map(reader));
        return tasks;
    }

    public async Task<bool> UpdateAsync(TaskItem task, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(task);
        await using var connection = await connections.OpenAsync(cancellationToken);
        using var command = connection.CreateCommand();
        command.CommandText = """
            UPDATE Tasks SET Title = @title, Description = @description,
                Status = @status, DueDate = @dueDate
            WHERE Id = @id AND UserId = @userId;
            """;
        AddTaskParameters(command, task);
        command.Parameters.AddWithValue("@id", task.Id);
        return await command.ExecuteNonQueryAsync(cancellationToken) > 0;
    }

    public async Task<bool> DeleteAsync(int taskId, int userId, CancellationToken cancellationToken)
    {
        await using var connection = await connections.OpenAsync(cancellationToken);
        using var command = connection.CreateCommand();
        command.CommandText = "DELETE FROM Tasks WHERE Id = @id AND UserId = @userId;";
        command.Parameters.AddWithValue("@id", taskId);
        command.Parameters.AddWithValue("@userId", userId);
        return await command.ExecuteNonQueryAsync(cancellationToken) > 0;
    }

    private static void AddTaskParameters(SqliteCommand command, TaskItem task)
    {
        command.Parameters.AddWithValue("@userId", task.UserId);
        command.Parameters.AddWithValue("@title", task.Title);
        command.Parameters.AddWithValue("@description", (object?)task.Description ?? DBNull.Value);
        command.Parameters.AddWithValue("@status", (int)task.Status);
        command.Parameters.AddWithValue("@dueDate", task.DueDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
    }

    private static TaskItem Map(SqliteDataReader reader) => new(
        reader.GetInt32(0), reader.GetInt32(1), reader.GetString(2),
        reader.IsDBNull(3) ? null : reader.GetString(3),
        DateOnly.ParseExact(reader.GetString(5), "yyyy-MM-dd", CultureInfo.InvariantCulture),
        (TaskStatus)reader.GetInt32(4));
}
