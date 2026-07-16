using FluentValidation;
using SmartTask.Application.DTOs;

namespace SmartTask.Application.Validators;

public class SmartParseRequestValidator : AbstractValidator<SmartParseRequestDto>
{
    public SmartParseRequestValidator()
    {
        RuleFor(x => x.Text)
            .NotEmpty().WithMessage("Text to parse cannot be empty.");
    }
}
