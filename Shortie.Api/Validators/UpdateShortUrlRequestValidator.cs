using FluentValidation;
using Shortie.Api.DTOs.Urls;

namespace Shortie.Api.Validators;

public class UpdateShortUrlRequestValidator : AbstractValidator<UpdateShortUrlRequestDto>
{
    public UpdateShortUrlRequestValidator()
    {
        RuleFor(x => x.OriginalUrl)
            .NotEmpty()
            .Must(BeValidAbsoluteUrl)
            .WithMessage("OriginalUrl must be a valid absolute URL.");

        RuleFor(x => x.CustomAlias)
            .MaximumLength(30)
            .Matches("^[a-zA-Z0-9_-]*$")
            .When(x => !string.IsNullOrWhiteSpace(x.CustomAlias))
            .WithMessage("CustomAlias can only contain letters, numbers, hyphen, and underscore.");

        RuleFor(x => x.ExpiresAtUtc)
            .GreaterThan(DateTime.UtcNow)
            .When(x => x.ExpiresAtUtc.HasValue)
            .WithMessage("Expiration date must be in the future.");
    }

    private static bool BeValidAbsoluteUrl(string value) =>
        Uri.TryCreate(value, UriKind.Absolute, out _);
}
