using FluentValidation;

namespace Grit.Modules.Catalog.Features.ChangeSkuPrice;

internal sealed class ChangeSkuPriceValidator : AbstractValidator<ChangeSkuPriceCommand>
{
    public ChangeSkuPriceValidator()
    {
        RuleFor(command => command.PriceAmount).GreaterThan(0);
        RuleFor(command => command.PriceCurrency).NotEmpty();
        RuleFor(command => command.Version).NotEmpty();
    }
}
