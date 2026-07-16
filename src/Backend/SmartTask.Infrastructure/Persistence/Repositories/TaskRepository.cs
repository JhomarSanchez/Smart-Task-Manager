using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SmartTask.Application.Interfaces.Persistence;
using SmartTask.Domain.Common;
using SmartTask.Domain.Entities;
using SmartTask.Domain.Enums;
using TaskStatus = SmartTask.Domain.Enums.TaskStatus;

namespace SmartTask.Infrastructure.Persistence.Repositories;

public class TaskRepository : ITaskRepository
{
    private readonly AppDbContext _context;

    public TaskRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<TaskItem?> GetByIdAsync(TaskId id, CancellationToken cancellationToken)
    {
        return await _context.TaskItems
            .FirstOrDefaultAsync(t => t.Id == id, cancellationToken);
    }

    public async Task<IEnumerable<TaskItem>> GetAllAsync(CancellationToken cancellationToken)
    {
        return await _context.TaskItems
            .OrderBy(t => t.Status)
            .ThenBy(t => t.BoardPosition)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(TaskItem task, CancellationToken cancellationToken)
    {
        await _context.TaskItems.AddAsync(task, cancellationToken);
    }

    public Task UpdateAsync(TaskItem task, CancellationToken cancellationToken)
    {
        // EF Core changes are tracked, but we attach/update if necessary
        _context.Entry(task).State = EntityState.Modified;
        return Task.CompletedTask;
    }

    public Task DeleteAsync(TaskItem task, CancellationToken cancellationToken)
    {
        _context.TaskItems.Remove(task);
        return Task.CompletedTask;
    }

    public async Task<double> GetMaxPositionAsync(TaskStatus status, CancellationToken cancellationToken)
    {
        var maxPosition = await _context.TaskItems
            .Where(t => t.Status == status)
            .Select(t => (double?)t.BoardPosition)
            .MaxAsync(cancellationToken);

        return maxPosition ?? 0.0;
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        await _context.SaveChangesAsync(cancellationToken);
    }
}
