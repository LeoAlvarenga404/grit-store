using Grit.BuildingBlocks;
using Grit.SharedKernel;
using MediatR;

namespace Grit.Modules.Catalog.Features.RemoveImage;

internal sealed record RemoveImageCommand(Guid ProductId, string FileName, uint Version) : IRequest<Result<ProductVersionResponse>>, ICommand;
