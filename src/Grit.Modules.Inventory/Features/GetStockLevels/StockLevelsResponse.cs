namespace Grit.Modules.Inventory.Features.GetStockLevels;

internal sealed record StockLevelsResponse(IReadOnlyList<StockLevelResponse> Items);
