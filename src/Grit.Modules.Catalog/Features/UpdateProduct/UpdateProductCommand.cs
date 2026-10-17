using Grit.BuildingBlocks;
using Grit.SharedKernel;
using MediatR;

namespace Grit.Modules.Catalog.Features.UpdateProduct;

internal sealed record UpdateProductCommand(
    Guid Id,
    Guid CategoryId,
    string Name,
    string Description,
    IReadOnlyDictionary<string, string> Attributes,
    uint Version) : IRequest<Result<ProductVersionResponse>>, ICommand;
