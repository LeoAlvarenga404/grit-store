using Grit.BuildingBlocks;
using Grit.Modules.Basket;
using Grit.Modules.Catalog;
using Grit.Modules.Identity;
using Grit.Modules.Inventory;
using Grit.Modules.Ordering;
using Grit.Modules.Payments;

namespace Grit.Api;

internal static class ModuleCatalog
{
    private static readonly IModule[] ProductionModules = [new CatalogModule(), new IdentityModule(), new BasketModule(), new InventoryModule(), new OrderingModule(), new PaymentsModule()];

    // Test hosts add the test-only probe module by type name; the setting is read before any module is composed.
    public static IReadOnlyList<IModule> Load(IConfiguration configuration)
    {
        var additionalTypeNames = configuration.GetSection("Modules:Additional").Get<string[]>() ?? [];
        var additionalModules = additionalTypeNames
            .Select(typeName => (IModule)Activator.CreateInstance(Type.GetType(typeName, throwOnError: true)!)!);
        return [.. ProductionModules, .. additionalModules];
    }
}
