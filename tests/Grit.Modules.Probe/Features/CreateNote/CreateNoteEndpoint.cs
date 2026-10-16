using Grit.BuildingBlocks;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;

namespace Grit.Modules.Probe.Features.CreateNote;

internal static class CreateNoteEndpoint
{
    public static void MapCreateNote(this IEndpointRouteBuilder notes) =>
        notes.MapPost("/", CreateNote)
            .WithSummary("Creates a note.")
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status409Conflict);

    private static async Task<Results<Created<CreateNoteResponse>, ProblemHttpResult>> CreateNote(
        CreateNoteRequest request,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new CreateNoteCommand(request.Text), cancellationToken);
        if (result.IsFailure)
        {
            return result.Error.ToProblem();
        }

        return TypedResults.Created($"/api/v1/probe/notes/{result.Value}", new CreateNoteResponse(result.Value));
    }
}
