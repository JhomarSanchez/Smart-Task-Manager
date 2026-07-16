using FluentValidation;
using SmartTask.Application.DTOs;
using System;

namespace SmartTask.Application.Validators;

public class UpdateTaskValidator : AbstractValidator<UpdateTaskDto>
{
    // Valid status values accepted from the frontend (mapped to domain enum)
    private static readonly string[] ValidStatuses = ["Todo", "InProgress", "Doing", "Done"];

    public UpdateTaskValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Title cannot be empty.")
            .MinimumLength(3).WithMessage("Title must be at least 3 characters.")
            .MaximumLength(100).WithMessage("Title must not exceed 100 characters.");

        RuleFor(x => x.Description)
            .MaximumLength(1000).WithMessage("Description must not exceed 1000 characters.");

        RuleFor(x => x.DueDate)
            .Must(dueDate => !dueDate.HasValue || dueDate.Value > DateTimeOffset.UtcNow)
            .WithMessage("Due date must be in the future.");

        // Category can now contain board:column format (up to 100 chars)
        RuleFor(x => x.Category)
            .MaximumLength(100).WithMessage("Category must not exceed 100 characters.");

        RuleFor(x => x.Priority)
            .Must(p => string.Equals(p, "Low", StringComparison.OrdinalIgnoreCase) ||
                       string.Equals(p, "Medium", StringComparison.OrdinalIgnoreCase) ||
                       string.Equals(p, "High", StringComparison.OrdinalIgnoreCase))
            .WithMessage("Priority must be Low, Medium, or High.");

        // Accept "Doing" as an alias for "InProgress" from the frontend
        RuleFor(x => x.Status)
            .Must(s => ValidStatuses.Any(v => string.Equals(s, v, StringComparison.OrdinalIgnoreCase)))
            .WithMessage("Status must be Todo, Doing, InProgress, or Done.");

        RuleFor(x => x.BoardPosition)
            .Must(bp => double.IsFinite(bp) && bp >= 0)
            .WithMessage("BoardPosition must be a non-negative finite value.");
    }
}
