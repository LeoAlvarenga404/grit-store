using Grit.Modules.Probe.Domain;
using Grit.Modules.Probe.Infrastructure;
using Grit.SharedKernel;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Grit.Modules.Probe.Features.GetNote;

internal sealed class GetNoteHandler(ProbeDbContext db) : IRequestHandler<GetNoteQuery, Result<GetNoteResponse>>
{
    public async Task<Result<GetNoteResponse>> Handle(GetNoteQuery query, CancellationToken cancellationToken)
    {
        var id = new NoteId(query.Id);
        var note = await db.Notes
            .AsNoTracking()
            .Where(note => note.Id == id)
            .Select(note => new GetNoteResponse(note.Id.Value, note.Text))
            .SingleOrDefaultAsync(cancellationToken);

        return note is null ? ProbeErrors.NoteNotFound : note;
    }
}
