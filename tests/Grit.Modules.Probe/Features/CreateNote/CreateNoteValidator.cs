using FluentValidation;

namespace Grit.Modules.Probe.Features.CreateNote;

internal sealed class CreateNoteValidator : AbstractValidator<CreateNoteCommand>
{
    public CreateNoteValidator()
    {
        RuleFor(command => command.Text).NotEmpty().MaximumLength(100);
    }
}
