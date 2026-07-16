using System;
using SmartTask.Domain.Common;
using SmartTask.Domain.Enums;
using TaskStatus = SmartTask.Domain.Enums.TaskStatus;

namespace SmartTask.Domain.Entities;

public class TaskItem
{
    public TaskId Id { get; private set; }
    public string Title { get; private set; } = null!;
    public string? Description { get; private set; }
    public DateTimeOffset? DueDate { get; private set; }
    public TaskPriority Priority { get; private set; }
    public string? Category { get; private set; }
    public TaskStatus Status { get; private set; }
    public double BoardPosition { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? UpdatedAt { get; private set; }

    // Required for EF Core deserialization
    private TaskItem() { }

    public TaskItem(
        TaskId id, 
        string title, 
        string? description, 
        DateTimeOffset? dueDate, 
        TaskPriority priority, 
        string? category, 
        double boardPosition)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Title cannot be empty.", nameof(title));

        Id = id;
        Title = title;
        Description = description;
        DueDate = dueDate;
        Priority = priority;
        Category = category;
        Status = TaskStatus.Todo; // All tasks start in the ToDo column
        BoardPosition = boardPosition;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public void UpdateDetails(
        string title, 
        string? description, 
        DateTimeOffset? dueDate, 
        TaskPriority priority, 
        string? category)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Title cannot be empty.", nameof(title));

        Title = title;
        Description = description;
        DueDate = dueDate;
        Priority = priority;
        Category = category;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void UpdateStatus(TaskStatus status)
    {
        Status = status;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void UpdatePosition(double boardPosition)
    {
        BoardPosition = boardPosition;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}
