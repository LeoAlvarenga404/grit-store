using Grit.BuildingBlocks;
using Grit.SharedKernel;
using MediatR;

namespace Grit.Modules.Catalog.Features.CreateProduct;

internal sealed record CreateProductCommand(
    Guid CategoryId,
    string Name,
    string Description,
    IReadOnlyDictionary<string, string> Attributes) : IRequest<Result<CreateProductResponse>>, ICommand;
