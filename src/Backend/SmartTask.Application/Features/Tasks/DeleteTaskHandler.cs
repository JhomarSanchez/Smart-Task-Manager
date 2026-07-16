using System;
using System.Threading;
using System.Threading.Tasks;
using SmartTask.Application.Interfaces.Persistence;
using SmartTask.Domain.Common;
using SmartTask.Domain.Common.Errors;

namespace SmartTask.Application.Features.Tasks;

public class DeleteTaskHandler
{
    private readonly ITaskRepository _repository;

    public DeleteTaskHandler(ITaskRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result> HandleAsync(Guid id, CancellationToken cancellationToken)
    {
        var task = await _repository.GetByIdAsync(new TaskId(id), cancellationToken);
        if (task is null)
        {
            return Result.Failure(Error.NotFound("Task.NotFound", $"Task with ID '{id}' was not found."));
        }

        await _repository.DeleteAsync(task, cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
