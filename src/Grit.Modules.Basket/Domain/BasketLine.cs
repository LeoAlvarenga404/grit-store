using Grit.SharedKernel;

namespace Grit.Modules.Basket.Domain;

internal sealed record BasketLine(Guid SkuId, int Quantity, Money UnitPrice, string ProductName, string ProductSlug);
