using Grit.SharedKernel;
using MediatR;

namespace Grit.Modules.Catalog.Features.GetProductFacets;

internal sealed record GetProductFacetsQuery(
    string? Query,
    string? Category,
    decimal? MinPrice,
    decimal? MaxPrice,
    IReadOnlyList<string> Attributes) : IRequest<Result<GetProductFacetsResponse>>, IProductFilter;
