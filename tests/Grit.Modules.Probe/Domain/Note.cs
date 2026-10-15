using Grit.SharedKernel;

namespace Grit.Modules.Probe.Domain;

internal sealed class Note : AggregateRoot<NoteId>
{
    private Note(NoteId id, string text) : base(id)
    {
        Text = text;
    }

    public string Text { get; }

    public static Note Create(string text) => new(NoteId.New(), text);
}
