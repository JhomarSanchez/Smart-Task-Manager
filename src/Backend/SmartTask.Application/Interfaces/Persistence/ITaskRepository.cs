using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SmartTask.Domain.Common;
using SmartTask.Domain.Entities;
using SmartTask.Domain.Enums;
using TaskStatus = SmartTask.Domain.Enums.TaskStatus;

namespace SmartTask.Application.Interfaces.Persistence;

public interface ITaskRepository
{
    Task<TaskItem?> GetByIdAsync(TaskId id, CancellationToken cancellationToken);
    Task<IEnumerable<TaskItem>> GetAllAsync(CancellationToken cancellationToken);
    Task AddAsync(TaskItem task, CancellationToken cancellationToken);
    Task UpdateAsync(TaskItem task, CancellationToken cancellationToken);
    Task DeleteAsync(TaskItem task, CancellationToken cancellationToken);
    Task<double> GetMaxPositionAsync(TaskStatus status, CancellationToken cancellationToken);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
