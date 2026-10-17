using FluentValidation;

namespace Grit.Modules.Identity.Features.RefreshSession;

internal sealed class RefreshSessionValidator : AbstractValidator<RefreshSessionRequest>
{
    public RefreshSessionValidator()
    {
        RuleFor(request => request.RefreshToken).NotEmpty().MaximumLength(100);
    }
}
