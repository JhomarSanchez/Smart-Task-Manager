using FluentValidation;
using SmartTask.Application.DTOs;
using System;

namespace SmartTask.Application.Validators;

public class CreateTaskValidator : AbstractValidator<CreateTaskDto>
{
    public CreateTaskValidator()
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

        RuleFor(x => x.Category)
            .MaximumLength(50).WithMessage("Category must not exceed 50 characters.");

        RuleFor(x => x.Priority)
            .Must(p => string.Equals(p, "Low", StringComparison.OrdinalIgnoreCase) ||
                       string.Equals(p, "Medium", StringComparison.OrdinalIgnoreCase) ||
                       string.Equals(p, "High", StringComparison.OrdinalIgnoreCase))
            .WithMessage("Priority must be Low, Medium, or High.");
    }
}
