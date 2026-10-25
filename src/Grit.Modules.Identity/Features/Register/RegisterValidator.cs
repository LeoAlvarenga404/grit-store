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
        // Composition is checked here and not in the Identity options, so the development admin seed keeps working with any password.
        RuleFor(command => command.Password)
            .Must(password => password is null || password.Any(char.IsLower)).WithMessage("Must contain a lowercase letter.")
            .Must(password => password is null || password.Any(char.IsUpper)).WithMessage("Must contain an uppercase letter.")
            .Must(password => password is null || password.Any(char.IsDigit)).WithMessage("Must contain a digit.")
            .Must(password => password is null || password.Any(character => !char.IsLetterOrDigit(character))).WithMessage("Must contain a symbol.");
    }
}
