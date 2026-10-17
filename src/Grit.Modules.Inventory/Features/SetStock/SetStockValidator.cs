using FluentValidation;

namespace Grit.Modules.Inventory.Features.SetStock;

internal sealed class SetStockValidator : AbstractValidator<SetStockCommand>
{
    public SetStockValidator()
    {
        RuleFor(command => command.OnHand).GreaterThanOrEqualTo(0);
    }
}
