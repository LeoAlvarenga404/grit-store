using Grit.SharedKernel;
using MediatR;

namespace Grit.Modules.Identity.Features.RefreshSession;

// Not an ICommand: a detected reuse must still commit the family revocation, which the ambient transaction would roll back
// with the failed Result. The handler runs its own transaction instead.
internal sealed record RefreshSessionRequest(string RefreshToken) : IRequest<Result<TokenResponse>>;
