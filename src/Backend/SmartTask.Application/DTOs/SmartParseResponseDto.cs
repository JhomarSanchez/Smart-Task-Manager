using System;

namespace SmartTask.Application.DTOs;

public record SmartParseResponseDto(
    bool IsParsed,
    string Title,
    string? Description,
    DateTimeOffset? DueDate,
    string Priority,
    string? Category
);
