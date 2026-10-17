using Grit.SharedKernel;
using MediatR;

namespace Grit.Modules.Identity.Features.LogOut;

// Not an ICommand: like the refresh, it takes the family lock inside a transaction of its own.
internal sealed record LogOutRequest(string RefreshToken) : IRequest<Result>;
