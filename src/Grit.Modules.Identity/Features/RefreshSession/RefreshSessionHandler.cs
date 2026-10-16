using Grit.Modules.Identity.Domain;
using Grit.Modules.Identity.Infrastructure;
using Grit.SharedKernel;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Grit.Modules.Identity.Features.RefreshSession;

internal sealed class RefreshSessionHandler(UsersDbContext db, TokenIssuer tokens)
    : IRequestHandler<RefreshSessionRequest, Result<TokenResponse>>
{
    public async Task<Result<TokenResponse>> Handle(RefreshSessionRequest request, CancellationToken cancellationToken)
    {
        var hash = RefreshToken.Hash(request.RefreshToken);
        var now = DateTimeOffset.UtcNow;
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);

        // The conditional UPDATE takes the row lock: of two concurrent refreshes with one token only one changes the row;
        // the other waits, re-reads the row as replaced (READ COMMITTED) and is handled as reuse.
        var rotated = await db.RefreshTokens
            .Where(token => token.TokenHash == hash && token.ReplacedAt == null && token.RevokedAt == null && token.ExpiresAt > now)
            .ExecuteUpdateAsync(setters => setters.SetProperty(token => token.ReplacedAt, now), cancellationToken);
        var presented = await db.RefreshTokens.AsNoTracking().SingleOrDefaultAsync(token => token.TokenHash == hash, cancellationToken);

        var result = rotated == 1
            ? await RotateAsync(presented!, cancellationToken)
            : await RejectAsync(presented, now, cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return result;
    }

    private async Task<Result<TokenResponse>> RotateAsync(RefreshToken current, CancellationToken cancellationToken)
    {
        var user = await db.Users.SingleAsync(user => user.Id == current.UserId, cancellationToken);
        return await tokens.IssueAsync(user, current.FamilyId, cancellationToken);
    }

    // Unknown, expired or revoked tokens are just invalid; a token that was already replaced was replayed, so its whole family ends.
    private async Task<Result<TokenResponse>> RejectAsync(RefreshToken? presented, DateTimeOffset now, CancellationToken cancellationToken)
    {
        if (presented?.ReplacedAt is null)
        {
            return IdentityErrors.InvalidRefreshToken;
        }

        var familyId = presented.FamilyId;
        await db.RefreshTokens
            .Where(token => token.FamilyId == familyId && token.RevokedAt == null)
            .ExecuteUpdateAsync(setters => setters.SetProperty(token => token.RevokedAt, now), cancellationToken);
        return IdentityErrors.RefreshTokenReused;
    }
}
