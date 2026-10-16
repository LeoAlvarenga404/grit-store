using Grit.Modules.Probe.Contracts;
using Grit.Modules.Probe.Domain;
using Grit.Modules.Probe.Infrastructure;
using Grit.SharedKernel;
using MassTransit;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Grit.Modules.Probe.Features.CreateNote;

internal sealed class CreateNoteHandler(ProbeDbContext db, IPublishEndpoint publisher)
    : IRequestHandler<CreateNoteCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(CreateNoteCommand command, CancellationToken cancellationToken)
    {
        if (await db.Notes.AnyAsync(note => note.Text == command.Text, cancellationToken))
        {
            return ProbeErrors.DuplicateText;
        }

        var note = Note.Create(command.Text);
        db.Notes.Add(note);
        await publisher.Publish(new NoteCreated(note.Id.Value, note.Text), cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        return note.Id.Value;
    }
}
