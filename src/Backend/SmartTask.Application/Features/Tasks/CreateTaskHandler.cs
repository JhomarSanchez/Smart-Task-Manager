using System;
using System.Threading;
using System.Threading.Tasks;
using SmartTask.Application.DTOs;
using SmartTask.Application.Interfaces.Persistence;
using SmartTask.Domain.Common;
using SmartTask.Domain.Entities;
using SmartTask.Domain.Enums;
using TaskStatus = SmartTask.Domain.Enums.TaskStatus;

namespace SmartTask.Application.Features.Tasks;

public class CreateTaskHandler
{
    private readonly ITaskRepository _repository;

    public CreateTaskHandler(ITaskRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result<TaskResponseDto>> HandleAsync(CreateTaskDto dto, CancellationToken cancellationToken)
    {
        if (!Enum.TryParse<TaskPriority>(dto.Priority, true, out var priority))
        {
            priority = TaskPriority.Medium;
        }

        var maxPosition = await _repository.GetMaxPositionAsync(TaskStatus.Todo, cancellationToken);
        var boardPosition = maxPosition == 0.0 ? 1000.0 : maxPosition + 1000.0;

        var taskId = TaskId.New();
        var task = new TaskItem(
            taskId,
            dto.Title,
            dto.Description,
            dto.DueDate,
            priority,
            dto.Category,
            boardPosition
        );

        await _repository.AddAsync(task, cancellationToken);
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
