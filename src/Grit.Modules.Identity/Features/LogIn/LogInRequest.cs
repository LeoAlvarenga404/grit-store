using Grit.SharedKernel;
using MediatR;

namespace Grit.Modules.Identity.Features.LogIn;

// Not an ICommand: a wrong password must still commit the lockout count, which the ambient transaction would roll back with the failed Result.
internal sealed record LogInRequest(string Email, string Password) : IRequest<Result<TokenResponse>>;
