using FluentValidation;

namespace Grit.Modules.Catalog.Features.GetProductFacets;

internal sealed class GetProductFacetsValidator : AbstractValidator<GetProductFacetsQuery>
{
    public GetProductFacetsValidator()
    {
        Include(new ProductFilterValidator());
    }
}
