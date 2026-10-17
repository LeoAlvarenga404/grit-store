using Grit.BuildingBlocks;
using Grit.SharedKernel;
using MediatR;

namespace Grit.Modules.Catalog.Features.StartImageUpload;

internal sealed record StartImageUploadCommand(Guid ProductId, string ContentType, long SizeInBytes)
    : IRequest<Result<ImageUploadResponse>>, ICommand;
