using FluentValidation;

namespace Grit.Modules.Inventory.Features.GetStockLevels;

internal sealed class GetStockLevelsValidator : AbstractValidator<GetStockLevelsQuery>
{
    public const int MaxSkus = 100;

    public GetStockLevelsValidator()
    {
        RuleFor(query => query.SkuIds)
            .NotEmpty()
            .Must(skuIds => skuIds.Count <= MaxSkus)
            .WithMessage("Ask for 1 to 100 SKUs.");
    }
}
