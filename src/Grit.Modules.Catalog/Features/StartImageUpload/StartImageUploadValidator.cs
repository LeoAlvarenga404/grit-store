using FluentValidation;

namespace Grit.Modules.Catalog.Features.StartImageUpload;

internal sealed class StartImageUploadValidator : AbstractValidator<StartImageUploadCommand>
{
    public StartImageUploadValidator()
    {
        RuleFor(command => command.ContentType)
            .Must(contentType => contentType is not null && ImageRule.ExtensionByContentType.ContainsKey(contentType))
            .WithMessage(ImageRule.Message);
        RuleFor(command => command.SizeInBytes).InclusiveBetween(1, ImageRule.MaxSizeInBytes).WithMessage(ImageRule.Message);
    }
}
