using System;
using System.Threading;
using System.Threading.Tasks;
using SmartTask.Application.DTOs;
using SmartTask.Application.Interfaces.Persistence;
using SmartTask.Domain.Common;
using SmartTask.Domain.Common.Errors;

namespace SmartTask.Application.Features.Tasks;

public class GetTaskByIdHandler
{
    private readonly ITaskRepository _repository;

    public GetTaskByIdHandler(ITaskRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result<TaskResponseDto>> HandleAsync(Guid id, CancellationToken cancellationToken)
    {
        var task = await _repository.GetByIdAsync(new TaskId(id), cancellationToken);
        if (task is null)
        {
            return Result.Failure<TaskResponseDto>(Error.NotFound("Task.NotFound", $"Task with ID '{id}' was not found."));
        }

        var dto = new TaskResponseDto(
            task.Id.Value,
            task.Title,
            task.Description,
            task.DueDate,
            task.Priority.ToString(),
            task.Category,
            task.Status.ToString(),
            task.BoardPosition,
            task.CreatedAt,
            task.UpdatedAt
        );

        return Result.Success(dto);
    }
}
