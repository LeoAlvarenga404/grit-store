using System.Net;

namespace Grit.MlImporter.Tests;

public sealed class FamilyReaderTests : IDisposable
{
    private const string Api = "https://api.mercadolibre.com/";

    private readonly FixtureHandler _handler = new FixtureHandler()
        .Json($"{Api}products/MLB2000", "product-MLB2000.json")
        .Json($"{Api}products/MLB2001", "product-MLB2001.json")
        .Json($"{Api}products/MLB2002", "product-MLB2002.json")
        .Json($"{Api}products/MLB2003", "product-MLB2003.json")
        .Json($"{Api}products/MLB2004", "product-MLB2004.json")
        .Json($"{Api}products/MLB2010", "product-MLB2010.json")
        .Json($"{Api}products/MLB2099", "items-not-found.json", HttpStatusCode.NotFound);

    private readonly HttpClient _http;

    public FamilyReaderTests() => _http = new HttpClient(_handler, disposeHandler: false);

    [Fact]
    public async Task ReadLeavesAsync_Family_ReadsEachRelatedProductOnceBreadthFirst()
    {
        var leaves = await Reader().ReadLeavesAsync("MLB2001", TestContext.Current.CancellationToken);

        leaves.Select(leaf => leaf.Id).ShouldBe(["MLB2001", "MLB2003", "MLB2002", "MLB2004"]);
        _handler.Requests.Select(request => request.Url).ShouldBe(
        [
            $"{Api}products/MLB2001", $"{Api}products/MLB2000", $"{Api}products/MLB2003",
            $"{Api}products/MLB2002", $"{Api}products/MLB2004", $"{Api}products/MLB2099",
        ]);
    }

    [Fact]
    public async Task HasSeen_AfterReadingAFamily_KnowsEveryProductOfIt()
    {
        var reader = Reader();

        await reader.ReadLeavesAsync("MLB2001", TestContext.Current.CancellationToken);

        reader.HasSeen("MLB2004").ShouldBeTrue();
        reader.HasSeen("MLB2010").ShouldBeFalse();
    }

    [Fact]
    public async Task ReadLeavesAsync_StandaloneProduct_IsItsOwnLeaf()
    {
        var leaves = await Reader().ReadLeavesAsync("MLB2010", TestContext.Current.CancellationToken);

        leaves.ShouldHaveSingleItem().Id.ShouldBe("MLB2010");
    }

    [Fact]
    public async Task ReadLeavesAsync_PickerToAMissingProduct_SkipsIt()
    {
        var leaves = await Reader().ReadLeavesAsync("MLB2003", TestContext.Current.CancellationToken);

        leaves.Select(leaf => leaf.Id).ShouldNotContain("MLB2099");
        _handler.Requests.Select(request => request.Url).ShouldContain($"{Api}products/MLB2099");
    }

    [Fact]
    public async Task ReadLeavesAsync_FamilyAboveTheCap_StopsReading()
    {
        var leaves = await Reader(maxProductsPerFamily: 2).ReadLeavesAsync("MLB2001", TestContext.Current.CancellationToken);

        leaves.Select(leaf => leaf.Id).ShouldBe(["MLB2001"]);
        _handler.Requests.Count.ShouldBe(2);
    }

    public void Dispose()
    {
        _http.Dispose();
        _handler.Dispose();
    }

    private FamilyReader Reader(int maxProductsPerFamily = FamilyReader.MaxProductsPerFamily) =>
        new(new MercadoLivreClient(_http, "test-token", TimeSpan.Zero), TextWriter.Null, maxProductsPerFamily);
}
