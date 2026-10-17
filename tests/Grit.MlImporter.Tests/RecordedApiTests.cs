namespace Grit.MlImporter.Tests;

// Bodies recorded from the live API on 2026-10-17: a sneaker leaf (colour and size on the leaf, siblings in the
// pickers, no buy box) and its only listing.
public sealed class RecordedApiTests : IDisposable
{
    private const string Api = "https://api.mercadolibre.com/";

    private readonly FixtureHandler _handler = new FixtureHandler()
        .Json($"{Api}products/MLB66991284", "real-product-MLB66991284.json")
        .Json($"{Api}products/MLB66991284/items", "real-items-MLB66991284.json")
        .Json($"{Api}products/MLB2001", "product-MLB2001.json")
        .Json($"{Api}products/MLB2002", "product-MLB2002.json")
        .Json($"{Api}products/MLB2003", "product-MLB2003.json")
        .Json($"{Api}products/MLB2004", "product-MLB2004.json")
        .Json(RecordedCatalog.SecondPage, "search-camisetas-50.json")
        .Json($"{Api}products/search?status=active&site_id=MLB&q=regata%20basica&limit=50&offset=0", "search-without-paging.json");

    private readonly HttpClient _http;

    public RecordedApiTests() => _http = new HttpClient(_handler, disposeHandler: false);

    [Fact]
    public async Task ProductAsync_RecordedLeaf_ReadsWhatTheImporterUses()
    {
        var leaf = (await Client().ProductAsync("MLB66991284", TestContext.Current.CancellationToken)).ShouldNotBeNull();

        leaf.ParentId.ShouldBe("MLB45036661");
        (leaf.ChildrenIds ?? []).ShouldBeEmpty();
        leaf.BuyBoxWinner.ShouldBeNull();
        leaf.FamilyName.ShouldBe("Tênis Corrida Olympikus Masculino 43506336 Sonoro 3");
        SeedMapper.SkuAttributes(leaf.Attributes!).ShouldBe(new Dictionary<string, string> { ["cor"] = "preto", ["tamanho"] = "41 BR" });
        leaf.Pickers!.Select(picker => picker.PickerId).ShouldBe(["COLOR", "FABRIC_DESIGN", "SIZE", "WIDTH_TYPE"]);
        leaf.Pickers![0].Products!.Select(product => product.ProductId).ShouldBe(["MLB66990915", "MLB66990846", "MLB66991284"]);
        leaf.Pictures!.Count.ShouldBe(8);
        leaf.Pictures!.ShouldAllBe(picture => picture.Url.StartsWith("https://", StringComparison.Ordinal));
    }

    [Fact]
    public async Task ItemsAsync_RecordedListings_GiveTheLowestBrlPrice()
    {
        var items = await Client().ItemsAsync("MLB66991284", TestContext.Current.CancellationToken);

        SeedMapper.LowestPriceInBrl(items).ShouldBe(299m);
    }

    [Theory]
    [InlineData("MLB2001", 79.9, "BRL")]
    [InlineData("MLB2003", 70.0, "USD")]
    [InlineData("MLB2004", 89.9, "BRL")]
    public async Task ProductAsync_LeafWithAWinner_ReadsItsPriceAndCurrency(string id, double price, string currency)
    {
        var winner = (await Client().ProductAsync(id, TestContext.Current.CancellationToken)).ShouldNotBeNull().BuyBoxWinner.ShouldNotBeNull();

        winner.Price.ShouldBe((decimal)price);
        winner.CurrencyId.ShouldBe(currency);
    }

    [Fact]
    public async Task ProductAsync_LeafWithoutAWinner_HasNoBuyBox()
    {
        (await Client().ProductAsync("MLB2002", TestContext.Current.CancellationToken)).ShouldNotBeNull().BuyBoxWinner.ShouldBeNull();
    }

    [Fact]
    public async Task SearchAsync_OffsetAndDomain_AsksForThatPageAndReadsItsPaging()
    {
        var page = await Client().SearchAsync("camiseta dry fit", "MLB-T_SHIRTS", 50, TestContext.Current.CancellationToken);

        _handler.Requests.Select(request => request.Url).ShouldBe([RecordedCatalog.SecondPage]);
        page.Results.Select(product => product.Id).ShouldBe(["MLB2020"]);
        page.Paging.ShouldBe(new MlPaging(60, 50, 50));
    }

    [Fact]
    public async Task SearchAsync_ResponseWithoutPaging_HasNoPaging()
    {
        var page = await Client().SearchAsync("regata basica", null, 0, TestContext.Current.CancellationToken);

        page.Results.Select(product => product.Id).ShouldBe(["MLB2010"]);
        page.Paging.ShouldBeNull();
    }

    public void Dispose()
    {
        _http.Dispose();
        _handler.Dispose();
    }

    private MercadoLivreClient Client() => new(_http, "test-token", TimeSpan.Zero);
}
