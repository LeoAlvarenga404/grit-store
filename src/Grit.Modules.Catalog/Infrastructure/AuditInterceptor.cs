using Grit.BuildingBlocks;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Grit.Modules.Catalog.Infrastructure;

// Audit columns live only on aggregate roots: a changed child stamps its root, which also bumps the root's xmin.
// The user comes from the current request; writes outside one (the seed) leave created_by/updated_by null.
internal sealed class AuditInterceptor(IHttpContextAccessor httpContextAccessor) : SaveChangesInterceptor
{
    public const string CreatedAt = "CreatedAt";

    public const string UpdatedAt = "UpdatedAt";

    public const string CreatedBy = "CreatedBy";

    public const string UpdatedBy = "UpdatedBy";

    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        Stamp(eventData);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        Stamp(eventData);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void Stamp(DbContextEventData eventData)
    {
        var changeTracker = eventData.Context?.ChangeTracker;
        if (changeTracker is null)
        {
            return;
        }

        var now = DateTimeOffset.UtcNow;
        var userId = httpContextAccessor.HttpContext?.User.GetUserId();

        // Entries() detects changes first, so a SKU just added to a product is already tracked as Added.
        foreach (var root in changeTracker.Entries().Where(entry => entry.Metadata.FindProperty(UpdatedAt) is not null).ToList())
        {
            if (root.State == EntityState.Added)
            {
                root.Property(CreatedAt).CurrentValue = now;
                root.Property(CreatedBy).CurrentValue = userId;
                root.Property(UpdatedAt).CurrentValue = now;
                root.Property(UpdatedBy).CurrentValue = userId;
                continue;
            }

            if (root.State == EntityState.Modified || HasChangedChild(root))
            {
                root.Property(UpdatedAt).CurrentValue = now;
                root.Property(UpdatedBy).CurrentValue = userId;
            }
        }
    }

    // ponytail: removed children leave the collection and go unseen; no use case deletes a SKU yet.
    private static bool HasChangedChild(EntityEntry root) =>
        root.Collections.Any(collection => collection.CurrentValue is { } children
            && children.Cast<object>().Any(child => root.Context.Entry(child).State is EntityState.Added or EntityState.Modified));
}
