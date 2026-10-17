using Grit.SharedKernel;
using MediatR;

namespace Grit.Modules.Catalog.Features.SearchProducts;

internal sealed record SearchProductsQuery(
    string? Query,
    string? Category,
    decimal? MinPrice,
    decimal? MaxPrice,
    IReadOnlyList<string> Attributes,
    string Sort,
    string? After,
    int Limit) : IRequest<Result<SearchProductsResponse>>, IProductFilter;
