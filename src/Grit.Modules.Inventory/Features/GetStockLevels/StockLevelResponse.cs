namespace Grit.Modules.Inventory.Features.GetStockLevels;

internal sealed record StockLevelResponse(Guid SkuId, int OnHand, int Held, int Available);
