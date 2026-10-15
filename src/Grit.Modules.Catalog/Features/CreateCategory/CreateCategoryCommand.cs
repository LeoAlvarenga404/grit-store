using Grit.BuildingBlocks;
using Grit.SharedKernel;
using MediatR;

namespace Grit.Modules.Catalog.Features.CreateCategory;

internal sealed record CreateCategoryCommand(string Name) : IRequest<Result<CreateCategoryResponse>>, ICommand;
