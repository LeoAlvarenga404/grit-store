using Grit.BuildingBlocks;
using Grit.SharedKernel;
using MediatR;

namespace Grit.Modules.Catalog.Features.AddSku;

internal sealed record AddSkuCommand(
    Guid ProductId,
    IReadOnlyDictionary<string, string> Attributes,
    decimal PriceAmount,
    string PriceCurrency,
    uint Version) : IRequest<Result<AddSkuResponse>>, ICommand;
