using FluentValidation;

namespace Grit.Modules.Identity.Features.LogIn;

internal sealed class LogInValidator : AbstractValidator<LogInRequest>
{
    public LogInValidator()
    {
        RuleFor(request => request.Email).NotEmpty().MaximumLength(256)
            .Must(email => email is null || !email.Contains('\0', StringComparison.Ordinal)).WithMessage("Must not contain the character U+0000 (NUL).");
        RuleFor(request => request.Password).NotEmpty().MaximumLength(128);
    }
}
