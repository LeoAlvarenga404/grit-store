using Grit.SharedKernel;
using MediatR;

namespace Grit.Modules.Catalog.Features.ListProducts;

internal sealed record ListProductsQuery(int Page) : IRequest<Result<ListProductsResponse>>;
