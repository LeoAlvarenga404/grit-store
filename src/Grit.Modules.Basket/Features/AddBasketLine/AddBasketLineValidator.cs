using FluentValidation;
using Grit.Modules.Basket.Domain;

namespace Grit.Modules.Basket.Features.AddBasketLine;

internal sealed class AddBasketLineValidator : AbstractValidator<AddBasketLineCommand>
{
    public AddBasketLineValidator()
    {
        RuleFor(command => command.SkuId).NotEmpty();
        RuleFor(command => command.Quantity).InclusiveBetween(1, CustomerBasket.MaxQuantityPerLine);
    }
}
