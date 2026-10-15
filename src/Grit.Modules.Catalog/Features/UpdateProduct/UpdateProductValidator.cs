using FluentValidation;

namespace Grit.Modules.Catalog.Features.UpdateProduct;

internal sealed class UpdateProductValidator : AbstractValidator<UpdateProductCommand>
{
    public UpdateProductValidator()
    {
        RuleFor(command => command.CategoryId).NotEmpty();
        RuleFor(command => command.Name).NotEmpty().MaximumLength(200).Must(TextRule.HasNoNullCharacter).WithMessage(TextRule.Message);
        RuleFor(command => command.Description).NotNull().MaximumLength(4000).Must(TextRule.HasNoNullCharacter).WithMessage(TextRule.Message);
        RuleFor(command => command.Attributes).Must(AttributesRule.IsValid).WithMessage(AttributesRule.Message);
        RuleFor(command => command.Version).NotEmpty();
    }
}
