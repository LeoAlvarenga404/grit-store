using System.Buffers.Text;
using System.Security.Cryptography;
using System.Text;

namespace Grit.Modules.Identity.Domain;

// Only the SHA-256 of a token is stored, so a copy of the table cannot be replayed as live sessions.
internal sealed class RefreshToken
{
    public static readonly TimeSpan Lifetime = TimeSpan.FromDays(7);

    private RefreshToken(Guid id, Guid userId, Guid familyId, byte[] tokenHash, DateTimeOffset createdAt, DateTimeOffset expiresAt)
    {
        Id = id;
        UserId = userId;
        FamilyId = familyId;
        TokenHash = tokenHash;
        CreatedAt = createdAt;
        ExpiresAt = expiresAt;
    }

    public Guid Id { get; }

    public Guid UserId { get; }

    public Guid FamilyId { get; }

    public byte[] TokenHash { get; }

    public DateTimeOffset CreatedAt { get; }

    public DateTimeOffset ExpiresAt { get; }

    // Set only by the set-based updates of the refresh handler (rotation and family revocation).
    public DateTimeOffset? ReplacedAt { get; }

    public DateTimeOffset? RevokedAt { get; }

    public static (RefreshToken Token, string Value) Issue(Guid userId, Guid familyId, DateTimeOffset now)
    {
        var value = Base64Url.EncodeToString(RandomNumberGenerator.GetBytes(32));
        var token = new RefreshToken(Guid.CreateVersion7(), userId, familyId, Hash(value), now, now + Lifetime);
        return (token, value);
    }

    public static byte[] Hash(string value) => SHA256.HashData(Encoding.UTF8.GetBytes(value));
}
