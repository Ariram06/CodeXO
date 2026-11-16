namespace Codexo.Api.Validators;

using Codexo.Core.Dtos.Preferences;
using FluentValidation;

public class UpdateUserPreferenceRequestValidator : AbstractValidator<UpdateUserPreferenceRequestDto>
{
    public UpdateUserPreferenceRequestValidator()
    {
        RuleFor(x => x.PreferredLanguage)
            .NotEmpty()
            .WithMessage("Preferred language is required.")
            .MaximumLength(50)
            .WithMessage("Preferred language cannot exceed 50 characters.");
    }
}
