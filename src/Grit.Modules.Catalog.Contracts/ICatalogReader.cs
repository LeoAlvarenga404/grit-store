namespace Grit.Modules.Catalog.Contracts;

public interface ICatalogReader
{
    Task<IReadOnlyList<SkuSnapshot>> GetSkusAsync(IReadOnlyCollection<Guid> skuIds, CancellationToken cancellationToken);

    Task<IReadOnlyList<Guid>> GetSkuIdsAsync(CancellationToken cancellationToken);
}
