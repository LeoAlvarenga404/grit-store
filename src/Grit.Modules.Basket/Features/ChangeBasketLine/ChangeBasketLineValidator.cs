using FluentValidation;
using Grit.Modules.Basket.Domain;

namespace Grit.Modules.Basket.Features.ChangeBasketLine;

internal sealed class ChangeBasketLineValidator : AbstractValidator<ChangeBasketLineCommand>
{
    public ChangeBasketLineValidator()
    {
        RuleFor(command => command.Quantity).InclusiveBetween(1, CustomerBasket.MaxQuantityPerLine);
    }
}
