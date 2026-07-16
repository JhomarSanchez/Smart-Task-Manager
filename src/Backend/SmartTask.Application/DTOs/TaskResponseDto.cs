using System;

namespace SmartTask.Application.DTOs;

public record TaskResponseDto(
    Guid Id,
    string Title,
    string? Description,
    DateTimeOffset? DueDate,
    string Priority,
    string? Category,
    string Status,
    double BoardPosition,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt
);
