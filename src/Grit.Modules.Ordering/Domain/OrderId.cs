namespace Grit.Modules.Ordering.Domain;

internal readonly record struct OrderId(Guid Value)
{
    public static OrderId New() => new(Guid.CreateVersion7());
}
