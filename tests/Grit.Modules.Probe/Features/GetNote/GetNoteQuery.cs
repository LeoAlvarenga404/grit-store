using Grit.SharedKernel;
using MediatR;

namespace Grit.Modules.Probe.Features.GetNote;

internal sealed record GetNoteQuery(Guid Id) : IRequest<Result<GetNoteResponse>>;
