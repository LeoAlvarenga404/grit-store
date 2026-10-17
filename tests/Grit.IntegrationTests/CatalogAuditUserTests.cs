using System.Net.Http.Headers;
using Grit.Modules.Catalog.Domain;
using Grit.Modules.Catalog.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Grit.IntegrationTests;

public class CatalogAuditUserTests(ApiFactory factory)
{
    [Fact]
    public async Task CreateCategory_AsAdmin_StampsTheAdminAsCreatorAndUpdater()
    {
        var adminId = Guid.NewGuid();

        var categoryId = await CatalogAdminApi.CreateCategoryAsync(AdminClient(adminId));

        (await CategoryAuditAsync(new CategoryId(categoryId))).ShouldBe((adminId, adminId));
    }

    [Fact]
    public async Task AddSku_ByAnotherAdmin_StampsTheUpdaterAndKeepsTheCreator()
    {
        var creatorId = Guid.NewGuid();
        var updaterId = Guid.NewGuid();
        var (productId, version) = await CatalogAdminApi.CreateProductAsync(AdminClient(creatorId));
        (await ProductAuditAsync(new ProductId(productId))).ShouldBe((creatorId, creatorId));

        await CatalogAdminApi.AddSkuAsync(AdminClient(updaterId), productId, version, "azul");

        (await ProductAuditAsync(new ProductId(productId))).ShouldBe((creatorId, updaterId));
    }

    // Seeds and other writes outside an HTTP request have no user.
    [Fact]
    public async Task SaveChanges_OutsideARequest_LeavesCreatorAndUpdaterNull()
    {
        var category = Category.Create(CatalogAdminApi.UniqueName("Categoria")).Value;
        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();
            db.Categories.Add(category);
            await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        (await CategoryAuditAsync(category.Id)).ShouldBe((null, null));
    }

    private HttpClient AdminClient(Guid adminId)
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", CustomerClient.Token(adminId, "admin"));
        return client;
    }

    private async Task<(Guid? CreatedBy, Guid? UpdatedBy)> CategoryAuditAsync(CategoryId id)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var row = await scope.ServiceProvider.GetRequiredService<CatalogDbContext>().Categories
            .Where(category => category.Id == id)
            .Select(category => new
            {
                CreatedBy = EF.Property<Guid?>(category, AuditInterceptor.CreatedBy),
                UpdatedBy = EF.Property<Guid?>(category, AuditInterceptor.UpdatedBy),
            })
            .SingleAsync(TestContext.Current.CancellationToken);
        return (row.CreatedBy, row.UpdatedBy);
    }

    private async Task<(Guid? CreatedBy, Guid? UpdatedBy)> ProductAuditAsync(ProductId id)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var row = await scope.ServiceProvider.GetRequiredService<CatalogDbContext>().Products
            .Where(product => product.Id == id)
            .Select(product => new
            {
                CreatedBy = EF.Property<Guid?>(product, AuditInterceptor.CreatedBy),
                UpdatedBy = EF.Property<Guid?>(product, AuditInterceptor.UpdatedBy),
            })
            .SingleAsync(TestContext.Current.CancellationToken);
        return (row.CreatedBy, row.UpdatedBy);
    }
}
