namespace Codexo.Api.Validators;

using Codexo.Core.Dtos.Preferences;
using FluentValidation;

public class UpdateUserPreferenceRequestValidator : AbstractValidator<UpdateUserPreferenceRequestDto>
{
    private static readonly string[] AllowedReasoningLevels = ["low", "medium", "high"];

    public UpdateUserPreferenceRequestValidator()
    {
        RuleFor(x => x.PreferredLanguage)
            .NotEmpty()
            .WithMessage("Preferred language is required.")
            .MaximumLength(50)
            .WithMessage("Preferred language cannot exceed 50 characters.");

        RuleFor(x => x.ReasoningLevel)
            .Must(level => string.IsNullOrWhiteSpace(level) || AllowedReasoningLevels.Contains(level.Trim().ToLowerInvariant()))
            .WithMessage("Reasoning level must be 'low', 'medium', or 'high'.");
    }
}
