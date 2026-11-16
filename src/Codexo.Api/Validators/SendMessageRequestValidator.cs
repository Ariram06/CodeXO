namespace Codexo.Api.Validators;

using Codexo.Core.Dtos.Chat;
using FluentValidation;

public class SendMessageRequestValidator : AbstractValidator<SendMessageRequest>
{
    public SendMessageRequestValidator()
    {
        RuleFor(x => x.Message)
            .NotEmpty().WithMessage("Message text is required.")
            .MaximumLength(4000).WithMessage("Message cannot exceed 4000 characters.");

        When(x => x.Context != null, () =>
        {
            RuleFor(x => x.Context!.Platform)
                .NotEmpty().WithMessage("Platform identifier is required.")
                .MaximumLength(64).WithMessage("Platform cannot exceed 64 characters.");

            RuleFor(x => x.Context!.Title)
                .NotEmpty().WithMessage("Title is required.")
                .MaximumLength(256).WithMessage("Title cannot exceed 256 characters.");

            RuleFor(x => x.Context!.Url)
                .NotEmpty().WithMessage("URL is required.")
                .MaximumLength(2048).WithMessage("URL cannot exceed 2048 characters.");

            RuleFor(x => x.Context!.Code)
                .MaximumLength(25000).WithMessage("Code cannot exceed 25,000 characters.")
                .When(x => !string.IsNullOrEmpty(x.Context!.Code));

            RuleFor(x => x.Context!.ProblemText)
                .MaximumLength(15000).WithMessage("Problem text cannot exceed 15,000 characters.")
                .When(x => !string.IsNullOrEmpty(x.Context!.ProblemText));

            RuleFor(x => x.Context!.Selection)
                .MaximumLength(5000).WithMessage("Selection cannot exceed 5000 characters.")
                .When(x => !string.IsNullOrEmpty(x.Context!.Selection));
        });
    }
}
