namespace Grit.MlImporter;

// Walks a catalog family breadth first: each product's parent, children and picker siblings. Ids are remembered for the
// whole run, so a search hit inside a family already read is not read again.
internal sealed class FamilyReader(MercadoLivreClient client, TextWriter log, int maxProductsPerFamily = FamilyReader.MaxProductsPerFamily)
{
    // ponytail: a cap per family keeps a runaway picker graph from spending the token's rate limit; a family cut short
    // loses variants (logged). Raise it if real families come close.
    public const int MaxProductsPerFamily = 150;

    private readonly HashSet<string> _seen = [];

    public bool HasSeen(string productId) => _seen.Contains(productId);

    // Leaves are the products without children: the variants, each with its own colour, size, pictures and listings.
    public async Task<IReadOnlyList<MlProduct>> ReadLeavesAsync(string productId, CancellationToken cancellationToken)
    {
        var leaves = new List<MlProduct>();
        var queue = new Queue<string>();
        Enqueue(queue, productId);
        for (var read = 0; queue.Count > 0; read++)
        {
            if (read == maxProductsPerFamily)
            {
                await log.WriteLineAsync($"  family of {productId} cut at {maxProductsPerFamily} products");
                break;
            }

            var id = queue.Dequeue();
            var product = await client.ProductAsync(id, cancellationToken);
            if (product is null)
            {
                await log.WriteLineAsync($"  skipped {id}: not found");
                continue;
            }

            foreach (var related in RelatedIds(product))
            {
                Enqueue(queue, related);
            }

            if (product.ChildrenIds is null or { Count: 0 })
            {
                leaves.Add(product);
            }
        }

        return leaves;
    }

    private void Enqueue(Queue<string> queue, string id)
    {
        if (_seen.Add(id))
        {
            queue.Enqueue(id);
        }
    }

    private static IEnumerable<string> RelatedIds(MlProduct product)
    {
        if (product.ParentId is not null)
        {
            yield return product.ParentId;
        }

        foreach (var child in product.ChildrenIds ?? [])
        {
            yield return child;
        }

        foreach (var sibling in (product.Pickers ?? []).SelectMany(picker => picker.Products ?? []))
        {
            yield return sibling.ProductId;
        }
    }
}
