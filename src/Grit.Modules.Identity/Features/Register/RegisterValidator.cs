using FluentValidation;

namespace Grit.Modules.Identity.Features.Register;

internal sealed class RegisterValidator : AbstractValidator<RegisterCommand>
{
    public RegisterValidator()
    {
        // PostgreSQL rejects U+0000 in text: without this rule it would surface as a 500 (ADR 0011).
        RuleFor(command => command.Email).NotEmpty().MaximumLength(256).EmailAddress()
            .Must(email => email is null || !email.Contains('\0', StringComparison.Ordinal)).WithMessage("Must not contain the character U+0000 (NUL).");
        RuleFor(command => command.Password).NotEmpty().Length(8, 128);
    }
}
