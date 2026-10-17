using FluentValidation;

namespace Grit.Modules.Catalog.Features.SearchProducts;

internal sealed class SearchProductsValidator : AbstractValidator<SearchProductsQuery>
{
    public const int MaxLimit = 50;

    public SearchProductsValidator()
    {
        Include(new ProductFilterValidator());
        RuleFor(query => query.Sort).Must(ProductSort.All.Contains).WithMessage(ProductSort.Message);
        RuleFor(query => query.After)
            .Must(after => after is null || ProductCursor.TryParse(after, out _))
            .WithMessage("Use the nextCursor of the previous page.");
        RuleFor(query => query.Limit).InclusiveBetween(1, MaxLimit);
    }
}
