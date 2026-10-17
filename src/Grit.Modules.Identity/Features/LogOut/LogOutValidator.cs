using FluentValidation;

namespace Grit.Modules.Identity.Features.LogOut;

internal sealed class LogOutValidator : AbstractValidator<LogOutRequest>
{
    public LogOutValidator()
    {
        RuleFor(request => request.RefreshToken).NotEmpty().MaximumLength(100);
    }
}
