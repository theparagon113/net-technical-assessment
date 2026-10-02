namespace TaskManager.Infrastructure.Persistence;

public sealed class SqliteDemoSeeder(SqliteConnectionFactory connections)
{
    // Local assessment credentials only; callers generate the hash through IPasswordHasher.
    public const string Username = "demo";
    public const string Password = "Demo123!";
    public async Task SeedAsync(string passwordHash, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(passwordHash);
        await using var connection = await connections.OpenAsync(cancellationToken);
        using var transaction = connection.BeginTransaction();
        using var userCommand = connection.CreateCommand();
        userCommand.Transaction = transaction;
        userCommand.CommandText = """
            INSERT INTO Users (Username, PasswordHash) VALUES (@username, @passwordHash)
            ON CONFLICT DO NOTHING
            RETURNING Id;
            """;
        userCommand.Parameters.AddWithValue("@username", Username);
        userCommand.Parameters.AddWithValue("@passwordHash", passwordHash);
        var id = await userCommand.ExecuteScalarAsync(cancellationToken);

        // Only a newly created demo user receives tasks. Reruns preserve edits and deletions.
        if (id is long userId)
        {
            var tasks = new[]
            {
                new TaskManager.Domain.TaskItem(0, checked((int)userId), "Review the assessment", null,
                    new DateOnly(2026, 10, 2)),
                new TaskManager.Domain.TaskItem(0, checked((int)userId), "Implement the task manager", "Finish the required milestones",
                    new DateOnly(2026, 10, 3), TaskManager.Domain.TaskStatus.InProgress),
                new TaskManager.Domain.TaskItem(0, checked((int)userId), "Plan the project", "Agree on scope and architecture",
                    new DateOnly(2026, 10, 1), TaskManager.Domain.TaskStatus.Completed)
            };
            foreach (var task in tasks)
            {
                using var taskCommand = connection.CreateCommand();
                taskCommand.Transaction = transaction;
                taskCommand.CommandText = """
                    INSERT INTO Tasks (UserId, Title, Description, Status, DueDate)
                    VALUES (@userId, @title, @description, @status, @dueDate);
                    """;
                taskCommand.Parameters.AddWithValue("@userId", task.UserId);
                taskCommand.Parameters.AddWithValue("@title", task.Title);
                taskCommand.Parameters.AddWithValue("@description", (object?)task.Description ?? DBNull.Value);
                taskCommand.Parameters.AddWithValue("@status", (int)task.Status);
                taskCommand.Parameters.AddWithValue("@dueDate", task.DueDate.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture));
                await taskCommand.ExecuteNonQueryAsync(cancellationToken);
            }
        }
        await transaction.CommitAsync(cancellationToken);
    }
}
