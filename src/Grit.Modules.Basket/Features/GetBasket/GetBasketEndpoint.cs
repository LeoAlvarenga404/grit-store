using System.Security.Claims;
using Grit.BuildingBlocks;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;

namespace Grit.Modules.Basket.Features.GetBasket;

internal static class GetBasketEndpoint
{
    public static void MapGetBasket(this IEndpointRouteBuilder basket) =>
        basket.MapGet("/", GetBasket)
            .WithSummary("Gets the customer's basket with the price seen when each item was added and the current price.")
            .WithDescription("hasPriceChanged flags items whose price changed since they were added; the subtotal uses current prices of available items. Send that subtotal as expectedSubtotalAmount at checkout.")
            .ProducesProblem(StatusCodes.Status401Unauthorized);

    private static async Task<Ok<BasketResponse>> GetBasket(ClaimsPrincipal user, ISender sender, CancellationToken cancellationToken)
    {
        var customerId = user.GetUserId() ?? throw new InvalidOperationException("An authorized request always carries the sub claim.");
        return TypedResults.Ok(await sender.Send(new GetBasketQuery(customerId), cancellationToken));
    }
}
