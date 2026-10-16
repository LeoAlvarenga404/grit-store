using MediatR;

namespace Grit.Modules.Basket.Features.GetBasket;

internal sealed record GetBasketQuery(Guid CustomerId) : IRequest<BasketResponse>;
