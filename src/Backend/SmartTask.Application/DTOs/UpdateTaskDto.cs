using System;

namespace SmartTask.Application.DTOs;

public record UpdateTaskDto(
    string Title,
    string? Description,
    DateTimeOffset? DueDate,
    string Priority,
    string? Category,
    string Status,
    double BoardPosition
);
