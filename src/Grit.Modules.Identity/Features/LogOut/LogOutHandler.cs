using Grit.Modules.Identity.Domain;
using Grit.Modules.Identity.Infrastructure;
using Grit.SharedKernel;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Grit.Modules.Identity.Features.LogOut;

// Always succeeds: an unknown or already revoked token gets the same answer, so logout does not tell which tokens exist.
internal sealed class LogOutHandler(UsersDbContext db) : IRequestHandler<LogOutRequest, Result>
{
    public async Task<Result> Handle(LogOutRequest request, CancellationToken cancellationToken)
    {
        var hash = RefreshToken.Hash(request.RefreshToken);
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);

        var familyId = await db.RefreshTokens.AsNoTracking()
            .Where(token => token.TokenHash == hash)
            .Select(token => (Guid?)token.FamilyId)
            .SingleOrDefaultAsync(cancellationToken);
        if (familyId is not null)
        {
            await db.LockFamilyAsync(familyId.Value, cancellationToken);
            var now = DateTimeOffset.UtcNow;
            await db.RefreshTokens
                .Where(token => token.FamilyId == familyId.Value && token.RevokedAt == null)
                .ExecuteUpdateAsync(setters => setters.SetProperty(token => token.RevokedAt, now), cancellationToken);
        }

        await transaction.CommitAsync(cancellationToken);
        return Result.Success();
    }
}
