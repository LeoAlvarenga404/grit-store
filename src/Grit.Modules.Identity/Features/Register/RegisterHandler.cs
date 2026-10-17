using Grit.Modules.Identity.Domain;
using Grit.Modules.Identity.Infrastructure;
using Grit.SharedKernel;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Grit.Modules.Identity.Features.Register;

internal sealed class RegisterHandler(UserManager<User> users) : IRequestHandler<RegisterCommand, Result<RegisterResponse>>
{
    private const string UserNameIndex = "UserNameIndex";

    public async Task<Result<RegisterResponse>> Handle(RegisterCommand command, CancellationToken cancellationToken)
    {
        var user = new User { Id = Guid.CreateVersion7(), UserName = command.Email, Email = command.Email };
        IdentityResult created;
        // Identity's duplicate check misses a concurrent double submit; the unique user-name index is then the only guard.
        try
        {
            created = await users.CreateAsync(user, command.Password);
        }
        catch (DbUpdateException exception) when (exception.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation, ConstraintName: UserNameIndex })
        {
            return IdentityErrors.EmailTaken;
        }

        if (created.Errors.Any(error => error.Code is "DuplicateUserName" or "DuplicateEmail"))
        {
            return IdentityErrors.EmailTaken;
        }

        ThrowIfFailed(created);
        ThrowIfFailed(await users.AddToRoleAsync(user, Roles.Customer));
        return new RegisterResponse(user.Id);
    }

    // The validator mirrors every other Identity rule (format, length, any character), so another failure is a bug.
    private static void ThrowIfFailed(IdentityResult result)
    {
        if (!result.Succeeded)
        {
            throw new InvalidOperationException(string.Join("; ", result.Errors.Select(error => $"{error.Code}: {error.Description}")));
        }
    }
}
