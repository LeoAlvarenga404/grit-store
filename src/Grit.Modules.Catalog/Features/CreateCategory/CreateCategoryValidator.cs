using FluentValidation;

namespace Grit.Modules.Catalog.Features.CreateCategory;

internal sealed class CreateCategoryValidator : AbstractValidator<CreateCategoryCommand>
{
    public CreateCategoryValidator()
    {
        RuleFor(command => command.Name).NotEmpty().MaximumLength(100);
    }
}
