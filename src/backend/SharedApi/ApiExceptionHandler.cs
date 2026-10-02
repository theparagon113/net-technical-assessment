using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using TaskManager.Application.Authentication;
using TaskManager.Application.Tasks;

namespace TaskManager.SharedApi;

internal sealed class ApiExceptionHandler(IProblemDetailsService problems) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext context, Exception exception, CancellationToken cancellationToken)
    {
        var (status, title) = exception switch
        {
            InvalidCredentialsException => (401, "Invalid username or password."),
            DuplicateUsernameException => (409, "Username is already registered."),
            TaskNotFoundException => (404, "Task not found."),
            ArgumentException => (400, "Invalid request input."),
            _ => (500, "An unexpected server error occurred.")
        };
        context.Response.StatusCode = status;
        await problems.WriteAsync(new ProblemDetailsContext
        {
            HttpContext = context,
            ProblemDetails = new ProblemDetails { Status = status, Title = title }
        });
        return true;
    }
}
