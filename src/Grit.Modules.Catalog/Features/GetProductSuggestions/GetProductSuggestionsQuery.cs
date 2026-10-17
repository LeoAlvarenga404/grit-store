using Grit.SharedKernel;
using MediatR;

namespace Grit.Modules.Catalog.Features.GetProductSuggestions;

internal sealed record GetProductSuggestionsQuery(string? Query) : IRequest<Result<IReadOnlyList<ProductSuggestionResponse>>>;
