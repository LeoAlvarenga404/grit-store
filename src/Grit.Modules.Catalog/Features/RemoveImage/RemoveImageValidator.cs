using FluentValidation;

namespace Grit.Modules.Catalog.Features.RemoveImage;

internal sealed class RemoveImageValidator : AbstractValidator<RemoveImageCommand>
{
    public RemoveImageValidator()
    {
        RuleFor(command => command.FileName).NotEmpty().MaximumLength(100);
        RuleFor(command => command.Version).NotEmpty();
    }
}
