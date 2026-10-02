using TaskManager.SharedApi;
using TaskManager.Application.Authentication;
using TaskManager.Application.Users;
using TaskManager.Infrastructure.Authentication;
using TaskManager.Infrastructure.Persistence;
using TaskManager.Infrastructure.Persistence.Repositories;

namespace TaskManager.Auth.Api;

public partial class Program
{
    public static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        builder.Services.AddApiFoundation(builder.Configuration);
        builder.Services.AddScoped<IUserRepository, SqliteUserRepository>();
        builder.Services.AddScoped<IPasswordHasher, FrameworkPasswordHasher>();
        builder.Services.AddScoped<ITokenService, JwtTokenService>();
        builder.Services.AddScoped<AuthService>();
        builder.Services.AddScoped<SqliteDemoSeeder>();
        var app = builder.Build();
        await app.InitializeDatabaseAsync(seedDemo: true);
        app.UseApiFoundation();
        await app.RunAsync();
    }
}
