namespace Grit.MlImporter.Tests;

// Bodies recorded from the live API on 2026-10-17: a sneaker leaf (colour and size on the leaf, siblings in the
// pickers, no buy box) and its only listing.
public sealed class RecordedApiTests : IDisposable
{
    private const string Api = "https://api.mercadolibre.com/";

    private readonly FixtureHandler _handler = new FixtureHandler()
        .Json($"{Api}products/MLB66991284", "real-product-MLB66991284.json")
        .Json($"{Api}products/MLB66991284/items", "real-items-MLB66991284.json");

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

    public void Dispose()
    {
        _http.Dispose();
        _handler.Dispose();
    }

    private MercadoLivreClient Client() => new(_http, "test-token", TimeSpan.Zero);
}
