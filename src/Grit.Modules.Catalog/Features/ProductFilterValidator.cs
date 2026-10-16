using FluentValidation;
using Grit.Modules.Catalog.Domain;

namespace Grit.Modules.Catalog.Features;

// Field names follow the query string: q and attribute, not Query and Attributes.
internal sealed class ProductFilterValidator : AbstractValidator<IProductFilter>
{
    public const int MaxQueryLength = 100;

    public ProductFilterValidator()
    {
        RuleFor(filter => filter.Query)
            .MaximumLength(MaxQueryLength)
            .Must(TextRule.HasNoNullCharacter).WithMessage(TextRule.Message)
            .OverridePropertyName("q");
        RuleFor(filter => filter.Category)
            .MaximumLength(Slug.MaxLength)
            .Must(TextRule.HasNoNullCharacter).WithMessage(TextRule.Message);
        RuleFor(filter => filter.MinPrice).GreaterThanOrEqualTo(0m);
        RuleFor(filter => filter.MaxPrice).GreaterThanOrEqualTo(0m);
        RuleFor(filter => filter.MaxPrice)
            .GreaterThanOrEqualTo(filter => filter.MinPrice)
            .When(filter => filter.MinPrice is not null);
        RuleFor(filter => filter.Attributes)
            .Must(AttributeFilter.IsValid).WithMessage(AttributeFilter.Message)
            .OverridePropertyName("attribute");
    }
}
