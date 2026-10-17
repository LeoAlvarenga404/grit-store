using FluentValidation;
using Grit.Modules.Ordering.Domain;

namespace Grit.Modules.Ordering.Features.ListAllOrders;

internal sealed class ListAllOrdersValidator : AbstractValidator<ListAllOrdersQuery>
{
    public const int MaxLimit = 100;

    public ListAllOrdersValidator()
    {
        // Names only: Enum.TryParse would also accept "1".
        RuleFor(query => query.Status)
            .Must(status => status is null || Enum.GetNames<OrderStatus>().Contains(status))
            .WithMessage("Use Placed, Confirmed, Cancelled or Shipped.");
        RuleFor(query => query.After).MaximumLength(20);
        RuleFor(query => query.Limit).InclusiveBetween(1, MaxLimit);
    }
}
