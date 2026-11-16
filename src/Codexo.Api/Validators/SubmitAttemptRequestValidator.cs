namespace Codexo.Api.Validators;

using Codexo.Core.Dtos.Hints;
using FluentValidation;

public class SubmitAttemptRequestValidator : AbstractValidator<SubmitAttemptRequestDto>
{
    public SubmitAttemptRequestValidator()
    {
        RuleFor(x => x.AttemptText)
            .NotEmpty().WithMessage("Attempt text is required.")
            .MaximumLength(5000).WithMessage("Attempt text must not exceed 5000 characters.");
    }
}
