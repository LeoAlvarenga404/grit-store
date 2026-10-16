using Grit.SharedKernel;
using MediatR;

namespace Grit.Modules.Catalog.Features.GetProductBySlug;

internal sealed record GetProductBySlugQuery(string Slug) : IRequest<Result<GetProductBySlugResponse>>;
