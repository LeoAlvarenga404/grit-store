namespace Grit.Modules.Probe.Domain;

internal readonly record struct NoteId(Guid Value)
{
    public static NoteId New() => new(Guid.CreateVersion7());
}
