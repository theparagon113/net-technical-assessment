using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Options;
using TaskManager.Application.Authentication;
using TaskManager.Infrastructure.Authentication;
using TaskManager.Infrastructure.Persistence;

namespace TaskManager.SharedApi;

internal static class ApiFoundation
{
    public static void AddApiFoundation(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("TaskManager");
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new InvalidOperationException("ConnectionStrings:TaskManager is required with an absolute SQLite file path.");
        SqliteConnectionStringBuilder database;
        try { database = new(connectionString); }
        catch (ArgumentException) { throw new InvalidOperationException("ConnectionStrings:TaskManager must be a valid SQLite connection string."); }
        if (string.IsNullOrWhiteSpace(database.DataSource) || !Path.IsPathFullyQualified(database.DataSource)
            || database.Mode == SqliteOpenMode.Memory)
            throw new InvalidOperationException("ConnectionStrings:TaskManager Data Source must be an absolute SQLite file path.");
        var jwt = configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>() ?? new();
        var validation = JwtValidation.Create(jwt);
        services.AddSingleton<IOptions<JwtOptions>>(Options.Create(jwt));
        services.AddSingleton(new SqliteConnectionFactory(database.ToString()));
        services.AddScoped<SqliteDatabaseInitializer>();
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(options =>
        {
            options.MapInboundClaims = false;
            options.IncludeErrorDetails = false;
            options.TokenValidationParameters = validation;
            options.Events = new JwtBearerEvents
            {
                OnTokenValidated = context =>
                {
                    if (!CurrentIdentity.TryGetUserId(context.Principal, out _)) context.Fail("Invalid identity.");
                    return Task.CompletedTask;
                }
            };
        });
        services.AddAuthorization();
        var origin = configuration["Cors:FrontendOrigin"] ?? "http://localhost:4200";
        if (!Uri.TryCreate(origin, UriKind.Absolute, out var uri) || uri.Scheme is not ("http" or "https")
            || uri.AbsolutePath != "/" || !string.IsNullOrEmpty(uri.Query) || !string.IsNullOrEmpty(uri.Fragment)
            || !string.IsNullOrEmpty(uri.UserInfo))
            throw new InvalidOperationException("Cors:FrontendOrigin must be an HTTP(S) origin.");
        services.AddCors(options => options.AddDefaultPolicy(policy =>
            policy.WithOrigins(uri.GetLeftPart(UriPartial.Authority)).AllowAnyHeader().AllowAnyMethod()));
        services.AddProblemDetails();
        services.AddExceptionHandler<ApiExceptionHandler>();
        services.AddControllers();
    }

    public static async Task InitializeDatabaseAsync(this WebApplication app, bool seedDemo)
    {
        var database = new SqliteConnectionStringBuilder(app.Configuration.GetConnectionString("TaskManager"));
        Directory.CreateDirectory(Path.GetDirectoryName(database.DataSource)!);
        using var scope = app.Services.CreateScope();
        await scope.ServiceProvider.GetRequiredService<SqliteDatabaseInitializer>().InitializeAsync();
        if (seedDemo)
        {
            var hash = scope.ServiceProvider.GetRequiredService<IPasswordHasher>().Hash(SqliteDemoSeeder.Password);
            await scope.ServiceProvider.GetRequiredService<SqliteDemoSeeder>().SeedAsync(hash);
        }
    }

    public static void UseApiFoundation(this WebApplication app)
    {
        app.UseExceptionHandler();
        app.UseStatusCodePages();
        app.UseHttpsRedirection();
        app.UseCors();
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapControllers();
    }
}
