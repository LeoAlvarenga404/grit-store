using FluentValidation;

namespace Grit.Modules.Catalog.Features.UpdateProduct;

internal sealed class UpdateProductValidator : AbstractValidator<UpdateProductCommand>
{
    public UpdateProductValidator()
    {
        RuleFor(command => command.CategoryId).NotEmpty();
        RuleFor(command => command.Name).NotEmpty().MaximumLength(200);
        RuleFor(command => command.Description).NotNull().MaximumLength(4000);
        RuleFor(command => command.Attributes).Must(AttributesRule.IsValid).WithMessage(AttributesRule.Message);
    }
}
