using TaskManager.Infrastructure.Persistence;
using TaskManager.Infrastructure.Persistence.Repositories;

namespace TaskManager.Infrastructure.Tests;

internal sealed class TestDatabase : IDisposable
{
    private readonly string path = Path.Combine(Path.GetTempPath(), $"task-manager-{Guid.NewGuid():N}.db");
    public SqliteConnectionFactory Connections { get; }
    public SqliteUserRepository Users { get; }
    public SqliteTaskRepository Tasks { get; }
    public SqliteDatabaseInitializer Initializer { get; }

    public TestDatabase()
    {
        // Real file lifetime spans the repositories' independent connections; no pool retains file handles.
        Connections = new($"Data Source={path};Pooling=False;Foreign Keys=False");
        Users = new(Connections);
        Tasks = new(Connections);
        Initializer = new(Connections);
    }

    public void Dispose() => File.Delete(path);
}
