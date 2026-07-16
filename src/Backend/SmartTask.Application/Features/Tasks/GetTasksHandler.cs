using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SmartTask.Application.DTOs;
using SmartTask.Application.Interfaces.Persistence;
using SmartTask.Domain.Common;

namespace SmartTask.Application.Features.Tasks;

public class GetTasksHandler
{
    private readonly ITaskRepository _repository;

    public GetTasksHandler(ITaskRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result<IEnumerable<TaskResponseDto>>> HandleAsync(CancellationToken cancellationToken)
    {
        var tasks = await _repository.GetAllAsync(cancellationToken);
        
        var sortedTasks = tasks
            .OrderBy(t => t.Status)
            .ThenBy(t => t.BoardPosition)
            .ThenBy(t => t.Id.Value)
            .Select(t => new TaskResponseDto(
                t.Id.Value,
                t.Title,
                t.Description,
                t.DueDate,
                t.Priority.ToString(),
                t.Category,
                t.Status.ToString(),
                t.BoardPosition,
                t.CreatedAt,
                t.UpdatedAt
            ));

        return Result.Success(sortedTasks);
    }
}
