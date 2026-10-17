using Grit.SharedKernel;

namespace Grit.Modules.Identity.Domain;

internal static class IdentityErrors
{
    public static readonly Error EmailTaken = new("identity.email_taken", "An account with this e-mail already exists.", ErrorType.Conflict);

    public static readonly Error InvalidCredentials = new("identity.invalid_credentials", "The e-mail or the password is wrong.", ErrorType.Unauthorized);

    public static readonly Error LockedOut = new("identity.locked_out", "Too many wrong passwords. Try again in 15 minutes.", ErrorType.Unauthorized);

    public static readonly Error InvalidRefreshToken = new("identity.invalid_refresh_token", "The refresh token is unknown, expired or revoked. Log in again.", ErrorType.Unauthorized);

    public static readonly Error RefreshTokenReused = new("identity.refresh_token_reused", "The refresh token was already used. Every session it started was ended. Log in again.", ErrorType.Unauthorized);
}
