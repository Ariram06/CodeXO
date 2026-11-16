namespace Codexo.Api.Validators;

using Codexo.Core.Dtos.Feedback;
using FluentValidation;

public class SubmitFeedbackRequestValidator : AbstractValidator<SubmitFeedbackRequestDto>
{
    public SubmitFeedbackRequestValidator()
    {
        RuleFor(x => x.Comment)
            .MaximumLength(500)
            .WithMessage("Feedback comment cannot exceed 500 characters.");
    }
}
