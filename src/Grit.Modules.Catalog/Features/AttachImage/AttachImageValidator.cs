using FluentValidation;

namespace Grit.Modules.Catalog.Features.AttachImage;

internal sealed class AttachImageValidator : AbstractValidator<AttachImageCommand>
{
    public AttachImageValidator()
    {
        RuleFor(command => command.Key).NotEmpty().MaximumLength(200);
        RuleFor(command => command.Color).MaximumLength(100).Must(TextRule.HasOnlySupportedCharacters).WithMessage(TextRule.Message);
        RuleFor(command => command.Version).NotEmpty();
    }
}
