using FluentValidation;

namespace Grit.Modules.Catalog.Features.CreateProduct;

internal sealed class CreateProductValidator : AbstractValidator<CreateProductCommand>
{
    public CreateProductValidator()
    {
        RuleFor(command => command.CategoryId).NotEmpty();
        RuleFor(command => command.Name).NotEmpty().MaximumLength(200).Must(TextRule.HasOnlySupportedCharacters).WithMessage(TextRule.Message);
        RuleFor(command => command.Description).NotNull().MaximumLength(4000).Must(TextRule.HasOnlySupportedCharacters).WithMessage(TextRule.Message);
        RuleFor(command => command.Attributes).Must(AttributesRule.IsValid).WithMessage(AttributesRule.Message);
    }
}
