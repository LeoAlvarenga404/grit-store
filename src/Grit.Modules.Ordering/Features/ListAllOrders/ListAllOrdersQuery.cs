using Grit.SharedKernel;
using MediatR;

namespace Grit.Modules.Ordering.Features.ListAllOrders;

internal sealed record ListAllOrdersQuery(string? Status, string? After, int Limit) : IRequest<Result<ListAllOrdersResponse>>;
