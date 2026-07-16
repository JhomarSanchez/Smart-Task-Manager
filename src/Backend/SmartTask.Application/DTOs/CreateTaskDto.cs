using System;

namespace SmartTask.Application.DTOs;

public record CreateTaskDto(
    string Title,
    string? Description,
    DateTimeOffset? DueDate,
    string Priority,
    string? Category
);
