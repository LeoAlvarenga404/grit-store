using FluentValidation;

namespace Grit.Modules.Catalog.Features.ListProducts;

internal sealed class ListProductsValidator : AbstractValidator<ListProductsQuery>
{
    public ListProductsValidator()
    {
        RuleFor(query => query.Page).InclusiveBetween(1, 100_000);
    }
}
