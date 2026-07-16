using System;
using System.Threading;
using System.Threading.Tasks;
using SmartTask.Application.DTOs;
using SmartTask.Application.Interfaces.Persistence;
using SmartTask.Domain.Common;
using SmartTask.Domain.Common.Errors;
using SmartTask.Domain.Enums;
using TaskStatus = SmartTask.Domain.Enums.TaskStatus;

namespace SmartTask.Application.Features.Tasks;

public class UpdateTaskHandler
{
    private readonly ITaskRepository _repository;

    public UpdateTaskHandler(ITaskRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result<TaskResponseDto>> HandleAsync(Guid id, UpdateTaskDto dto, CancellationToken cancellationToken)
    {
        var task = await _repository.GetByIdAsync(new TaskId(id), cancellationToken);
        if (task is null)
        {
            return Result.Failure<TaskResponseDto>(Error.NotFound("Task.NotFound", $"Task with ID '{id}' was not found."));
        }

        if (!Enum.TryParse<TaskPriority>(dto.Priority, true, out var priority))
        {
            priority = TaskPriority.Medium;
        }

        if (!Enum.TryParse<TaskStatus>(dto.Status, true, out var status))
        {
            status = TaskStatus.Todo;
        }

        task.UpdateDetails(dto.Title, dto.Description, dto.DueDate, priority, dto.Category);
        task.UpdateStatus(status);
        task.UpdatePosition(dto.BoardPosition);

        await _repository.UpdateAsync(task, cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);

        var response = new TaskResponseDto(
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

        return Result.Success(response);
    }
}
