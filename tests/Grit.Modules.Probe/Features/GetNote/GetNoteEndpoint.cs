using Grit.BuildingBlocks;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;

namespace Grit.Modules.Probe.Features.GetNote;

internal static class GetNoteEndpoint
{
    public static void MapGetNote(this IEndpointRouteBuilder notes) =>
        notes.MapGet("/{id:guid}", GetNote)
            .WithSummary("Gets a note by id.")
            .ProducesProblem(StatusCodes.Status404NotFound);

    private static async Task<Results<Ok<NoteResponse>, ProblemHttpResult>> GetNote(
        Guid id,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetNoteQuery(id), cancellationToken);
        if (result.IsFailure)
        {
            return result.Error.ToProblem();
        }

        return TypedResults.Ok(result.Value);
    }
}
