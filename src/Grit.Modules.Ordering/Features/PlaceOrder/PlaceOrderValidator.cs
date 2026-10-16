using FluentValidation;

namespace Grit.Modules.Ordering.Features.PlaceOrder;

internal sealed class PlaceOrderValidator : AbstractValidator<PlaceOrderCommand>
{
    public PlaceOrderValidator()
    {
        RuleFor(command => command.IdempotencyKey).NotEmpty().MaximumLength(100);
        RuleFor(command => command.PaymentToken).NotEmpty().MaximumLength(100);
        RuleFor(command => command.ExpectedSubtotalAmount).GreaterThanOrEqualTo(0);
    }
}
