using Grit.BuildingBlocks;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;

namespace Grit.Modules.Identity.Features.Register;

internal static class RegisterEndpoint
{
    public static void MapRegister(this IEndpointRouteBuilder identity) =>
        identity.MapPost("/register", Register)
            .WithSummary("Registers a customer.")
            .WithDescription("Creates an account with the customer role. The password needs 8 to 128 characters with a lowercase letter, an uppercase letter, a digit and a symbol. Log in afterwards to get tokens.")
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status409Conflict);

    private static async Task<Results<Created<RegisterResponse>, ProblemHttpResult>> Register(
        RegisterRequest request,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new RegisterCommand(request.Email, request.Password), cancellationToken);
        if (result.IsFailure)
        {
            return result.Error.ToProblem();
        }

        return TypedResults.Created((string?)null, result.Value);
    }
}
