using Grit.Modules.Identity.Domain;
using Grit.Modules.Identity.Infrastructure;
using Grit.SharedKernel;
using MediatR;
using Microsoft.AspNetCore.Identity;

namespace Grit.Modules.Identity.Features.LogIn;

internal sealed class LogInHandler(UserManager<User> users, TokenIssuer tokens) : IRequestHandler<LogInRequest, Result<TokenResponse>>
{
    public async Task<Result<TokenResponse>> Handle(LogInRequest request, CancellationToken cancellationToken)
    {
        var user = await users.FindByEmailAsync(request.Email);
        if (user is null)
        {
            return IdentityErrors.InvalidCredentials;
        }

        if (await users.IsLockedOutAsync(user))
        {
            return IdentityErrors.LockedOut;
        }

        if (!await users.CheckPasswordAsync(user, request.Password))
        {
            // ponytail: two concurrent failures can count once (ConcurrencyStamp conflict); the login rate limit bounds the loss.
            await users.AccessFailedAsync(user);
            return await users.IsLockedOutAsync(user) ? IdentityErrors.LockedOut : IdentityErrors.InvalidCredentials;
        }

        await users.ResetAccessFailedCountAsync(user);
        return await tokens.IssueAsync(user, Guid.CreateVersion7(), cancellationToken);
    }
}
