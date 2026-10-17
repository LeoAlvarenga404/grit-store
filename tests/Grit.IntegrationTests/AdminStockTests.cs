using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Grit.Modules.Inventory.Domain;
using Grit.Modules.Inventory.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace Grit.IntegrationTests;

public class AdminStockTests(ApiFactory factory)
{
    private const string StockPath = "/api/v1/admin/stock";

    private readonly HttpClient _admin = AdminClient.Create(factory);

    [Fact]
    public async Task SetStock_SkuWithoutStock_CreatesItAndListsTheLevels()
    {
        var (_, _, skuId) = await BasketApi.CreateSkuAsync(_admin);

        var response = await SetAsync(skuId, 5);

        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        var levels = await _admin.GetFromJsonAsync<JsonElement>($"{StockPath}?skuId={skuId}&skuId={Guid.NewGuid()}", TestContext.Current.CancellationToken);
        var level = levels.GetProperty("items").EnumerateArray().ShouldHaveSingleItem();
        level.GetProperty("skuId").GetGuid().ShouldBe(skuId);
        level.GetProperty("onHand").GetInt32().ShouldBe(5);
        level.GetProperty("held").GetInt32().ShouldBe(0);
        level.GetProperty("available").GetInt32().ShouldBe(5);
    }

    [Fact]
    public async Task SetStock_BelowHeldUnits_Returns409AndKeepsOnHand()
    {
        var (_, _, skuId) = await BasketApi.CreateSkuAsync(_admin);
        await ReserveAsync(skuId, onHand: 5, quantity: 3);

        var response = await SetAsync(skuId, 2);

        var problem = await ProblemResponse.ReadAsync(response, HttpStatusCode.Conflict);
        problem.GetProperty("code").GetString().ShouldBe("inventory.below_held_units");
        (await InventoryStock.GetAsync(factory, skuId)).OnHand.ShouldBe(5);
    }

    [Fact]
    public async Task SetStock_UnknownSku_Returns404()
    {
        var response = await SetAsync(Guid.NewGuid(), 5);

        var problem = await ProblemResponse.ReadAsync(response, HttpStatusCode.NotFound);
        problem.GetProperty("code").GetString().ShouldBe("inventory.sku_not_found");
    }

    [Fact]
    public async Task SetStock_NegativeOnHand_Returns400()
    {
        var (_, _, skuId) = await BasketApi.CreateSkuAsync(_admin);

        var response = await SetAsync(skuId, -1);

        var problem = await ProblemResponse.ReadAsync(response, HttpStatusCode.BadRequest);
        problem.GetProperty("code").GetString().ShouldBe("validation.failed");
    }

    [Fact]
    public async Task GetStockLevels_CustomerToken_Returns403()
    {
        var customer = CustomerClient.Create(factory, Guid.NewGuid());

        var response = await customer.GetAsync($"{StockPath}?skuId={Guid.NewGuid()}", TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    private Task<HttpResponseMessage> SetAsync(Guid skuId, int onHand) =>
        _admin.PutAsJsonAsync($"{StockPath}/{skuId}", new { onHand }, TestContext.Current.CancellationToken);

    private async Task ReserveAsync(Guid skuId, int onHand, int quantity)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<InventoryDbContext>();
        var item = StockItem.Create(skuId, onHand);
        item.Reserve(Guid.NewGuid(), quantity, DateTimeOffset.UtcNow);
        db.StockItems.Add(item);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
    }
}
