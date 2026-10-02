using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskManager.SharedApi;
using TaskManager.Application.Tasks;

namespace TaskManager.Api.Tasks;

[ApiController]
[Authorize]
[Route("api/tasks")]
public sealed class TasksController(TaskService tasks) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<TaskResult>>> List(CancellationToken cancellationToken) =>
        Ok(await tasks.GetByUserIdAsync(CurrentIdentity.UserId(User), cancellationToken));

    [HttpGet("{id}")]
    public async Task<ActionResult<TaskResult>> Get(int id, CancellationToken cancellationToken) =>
        Ok(await tasks.GetByIdAsync(CurrentIdentity.UserId(User), id, cancellationToken));

    [HttpPost]
    public async Task<ActionResult<TaskResult>> Create(TaskRequest input, CancellationToken cancellationToken)
    {
        var result = await tasks.CreateAsync(CurrentIdentity.UserId(User), input.ToInput(), cancellationToken);
        return CreatedAtAction(nameof(Get), new { id = result.Id }, result);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<TaskResult>> Update(int id, TaskRequest input, CancellationToken cancellationToken) =>
        Ok(await tasks.UpdateAsync(CurrentIdentity.UserId(User), id, input.ToInput(), cancellationToken));

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        await tasks.DeleteAsync(CurrentIdentity.UserId(User), id, cancellationToken);
        return NoContent();
    }
}

