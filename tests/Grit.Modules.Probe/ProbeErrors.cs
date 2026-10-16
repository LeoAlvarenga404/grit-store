using Grit.SharedKernel;

namespace Grit.Modules.Probe;

internal static class ProbeErrors
{
    public static readonly Error DuplicateText = new("probe.duplicate_text", "A note with this text already exists.", ErrorType.Conflict);

    public static readonly Error NoteNotFound = new("probe.note_not_found", "The note does not exist.", ErrorType.NotFound);
}
