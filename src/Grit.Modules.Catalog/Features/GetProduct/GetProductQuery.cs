using Grit.SharedKernel;
using MediatR;

namespace Grit.Modules.Catalog.Features.GetProduct;

internal sealed record GetProductQuery(Guid Id) : IRequest<Result<GetProductResponse>>;
