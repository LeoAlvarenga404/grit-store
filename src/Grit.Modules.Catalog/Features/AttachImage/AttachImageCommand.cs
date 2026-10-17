using Grit.BuildingBlocks;
using Grit.SharedKernel;
using MediatR;

namespace Grit.Modules.Catalog.Features.AttachImage;

internal sealed record AttachImageCommand(Guid ProductId, string Key, uint Version) : IRequest<Result<ProductVersionResponse>>, ICommand;
