namespace Codexo.Api.Validators;

using Codexo.Core.DTOs.Problems;
using Codexo.Core.Enums;
using FluentValidation;

public class ProblemIntakeRequestValidator : AbstractValidator<ProblemIntakeRequest>
{
    public ProblemIntakeRequestValidator()
    {
        RuleFor(x => x.Platform)
            .IsInEnum().WithMessage("A valid DSA platform must be specified.")
            .NotEqual(Platform.Unknown).WithMessage("Platform cannot be Unknown.");

        RuleFor(x => x.PlatformProblemId)
            .NotEmpty().WithMessage("Platform problem identifier is required.")
            .MaximumLength(100).WithMessage("Platform problem ID must not exceed 100 characters.");

        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Problem title is required.")
            .MaximumLength(256).WithMessage("Problem title must not exceed 256 characters.");

        RuleFor(x => x.Slug)
            .NotEmpty().WithMessage("Problem slug is required.")
            .MaximumLength(256).WithMessage("Problem slug must not exceed 256 characters.")
            .Matches("^[a-zA-Z0-9\\-_]+$").WithMessage("Problem slug must contain only alphanumeric characters, dashes, or underscores.");

        RuleFor(x => x.Url)
            .NotEmpty().WithMessage("Problem URL is required.")
            .MaximumLength(2048).WithMessage("Problem URL must not exceed 2048 characters.")
            .Must(BeAValidUrl).WithMessage("Problem URL must be a valid absolute URL.");

        RuleFor(x => x.Description)
            .NotEmpty().WithMessage("Problem description is required.")
            .MinimumLength(10).WithMessage("Problem description must be at least 10 characters long.")
            .MaximumLength(20000).WithMessage("Problem description must not exceed 20,000 characters.");

        RuleFor(x => x.Difficulty)
            .MaximumLength(50).WithMessage("Difficulty must not exceed 50 characters.")
            .When(x => !string.IsNullOrEmpty(x.Difficulty));
    }

    private static bool BeAValidUrl(string url)
    {
        return Uri.TryCreate(url, UriKind.Absolute, out var uriResult)
            && (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps);
    }
}
