using Grit.BuildingBlocks;
using Grit.Modules.Catalog.Contracts;
using Grit.Modules.Catalog.Features.AddSku;
using Grit.Modules.Catalog.Features.AttachImage;
using Grit.Modules.Catalog.Features.ChangeSkuPrice;
using Grit.Modules.Catalog.Features.CreateCategory;
using Grit.Modules.Catalog.Features.CreateProduct;
using Grit.Modules.Catalog.Features.GetProduct;
using Grit.Modules.Catalog.Features.GetProductBySlug;
using Grit.Modules.Catalog.Features.GetProductFacets;
using Grit.Modules.Catalog.Features.GetProductSuggestions;
using Grit.Modules.Catalog.Features.ListProducts;
using Grit.Modules.Catalog.Features.RemoveImage;
using Grit.Modules.Catalog.Features.SearchProducts;
using Grit.Modules.Catalog.Features.StartImageUpload;
using Grit.Modules.Catalog.Features.UpdateProduct;
using Grit.Modules.Catalog.Infrastructure;
using Grit.Modules.Catalog.Infrastructure.Seed;
using MassTransit;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Npgsql;

namespace Grit.Modules.Catalog;

public sealed class CatalogModule : IModule
{
    private static readonly AuditInterceptor Audit = new();

    public void AddServices(IHostApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.Services.AddDbContext<CatalogDbContext>((services, options) =>
            CatalogDbContext.Configure(options, services.GetRequiredService<NpgsqlDataSource>()).AddInterceptors(Audit));

        builder.Services.AddScoped<CatalogSeeder>();

        // Development only, like the migrations (ADR 0008): the AppHost points Seed:Path at seed/catalog.json.
        if (builder.Environment.IsDevelopment())
        {
            builder.Services.AddHostedService<CatalogSeedService>();
        }

        builder.Services.AddScoped<ICatalogReader, CatalogReader>();
        builder.Services.AddSingleton<ProductImageStorage>();
    }

    // Catalog publishes no integration event yet, and the single bus-outbox slot stays with the probe (ADR 0005).
    public void AddMessaging(IBusRegistrationConfigurator bus)
    {
    }

    public async Task MigrateDatabaseAsync(IServiceProvider services, CancellationToken cancellationToken)
    {
        await using var scope = services.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<CatalogDbContext>().Database.MigrateAsync(cancellationToken);

        // Provisioned like the schema, in Development only (ADR 0008); hosts without the storage (most tests) skip it.
        var images = scope.ServiceProvider.GetRequiredService<ProductImageStorage>();
        if (images.IsConfigured)
        {
            await images.EnsureBucketAsync(cancellationToken);
        }
    }

    public void MapEndpoints(IEndpointRouteBuilder api)
    {
        var admin = api.MapAdminGroup().WithTags("Catalog");
        admin.MapCreateCategory();

        var products = admin.MapGroup("/products");
        products.MapCreateProduct();
        products.MapGetProduct();
        products.MapUpdateProduct();
        products.MapAddSku();
        products.MapChangeSkuPrice();
        products.MapListProducts();
        products.MapStartImageUpload();
        products.MapAttachImage();
        products.MapRemoveImage();

        var catalog = api.MapGroup("/catalog").WithTags("Catalog");
        catalog.MapSearchProducts();
        catalog.MapGetProductFacets();
        catalog.MapGetProductSuggestions();
        catalog.MapGetProductBySlug();
    }
}
