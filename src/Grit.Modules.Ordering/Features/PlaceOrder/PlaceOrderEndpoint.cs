using System.Security.Claims;
using Grit.BuildingBlocks;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace Grit.Modules.Ordering.Features.PlaceOrder;

internal static class PlaceOrderEndpoint
{
    public static void MapPlaceOrder(this IEndpointRouteBuilder orders) =>
        orders.MapPost("/", PlaceOrder)
            .WithSummary("Places an order from the customer's basket (checkout).")
            .WithDescription("Send a new Idempotency-Key header per checkout attempt; repeating a key returns the same order. expectedSubtotalAmount is the basket subtotal the customer saw: a different current subtotal answers 409 ordering.price_changed. The order starts as Placed; poll GET /orders/{id} until Confirmed or Cancelled. The fake gateway declines the token tok_decline.")
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status409Conflict);

    // The basket and catalog reads open their own connections, so they run before the command's transaction (ADR 0005).
    private static async Task<Results<Created<PlaceOrderResponse>, ProblemHttpResult>> PlaceOrder(
        [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey,
        PlaceOrderRequest request,
        ClaimsPrincipal user,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var customerId = user.GetUserId() ?? throw new InvalidOperationException("An authorized request always carries the sub claim.");
        var lines = await sender.Send(new GetCheckoutLinesQuery(customerId), cancellationToken);
        if (lines.IsFailure)
        {
            return lines.Error.ToProblem();
        }

        var command = new PlaceOrderCommand(customerId, idempotencyKey ?? string.Empty, request.PaymentToken, request.ExpectedSubtotalAmount, lines.Value);
        var result = await sender.Send(command, cancellationToken);
        if (result.IsFailure)
        {
            return result.Error.ToProblem();
        }

        return TypedResults.Created($"/api/v1/orders/{result.Value.OrderId}", result.Value);
    }
}
