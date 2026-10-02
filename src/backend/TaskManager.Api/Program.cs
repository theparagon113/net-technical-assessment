using TaskManager.SharedApi;
using TaskManager.Application.Tasks;
using TaskManager.Infrastructure.Persistence.Repositories;

namespace TaskManager.Api;

public partial class Program
{
    public static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        builder.Services.AddApiFoundation(builder.Configuration);
        builder.Services.AddScoped<ITaskRepository, SqliteTaskRepository>();
        builder.Services.AddScoped<TaskService>();
        var app = builder.Build();
        await app.InitializeDatabaseAsync(seedDemo: false);
        app.UseApiFoundation();
        await app.RunAsync();
    }
}
