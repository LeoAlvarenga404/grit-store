using Grit.BuildingBlocks;
using Grit.SharedKernel;
using MediatR;

namespace Grit.Modules.Probe.Features.CreateNote;

internal sealed record CreateNoteCommand(string Text) : IRequest<Result<Guid>>, ICommand;
