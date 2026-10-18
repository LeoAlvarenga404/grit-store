using System.Net;
using System.Text;
using SkiaSharp;

namespace Grit.MlImporter.Tests;

// Synthetic world (the importer treats every domain alike). Two families behind the "camiseta dry fit" search: a parent
// with four leaves (one without a BRL offer) and a pair linked only by pickers; MLB2010 has a single colour and MLB2002
// repeats a leaf already read; MLB2020's picture is an error page. "tenis veloz" finds two parents joined by a picker;
// "tenis gemeos" adds a third parent with MLB2030's family name. Every picture is one flat marker colour, so a test can
// tell which picture ended up in which file.
internal static class RecordedCatalog
{
    public const string Api = "https://api.mercadolibre.com/";

    public const string Picture = "https://http2.mlstatic.com/D_NQ_NP_";

    public const string Category = "Camisetas e regatas=camiseta dry fit@MLB-T_SHIRTS";

    public const string SneakersCategory = "Tênis=tenis veloz@MLB-SNEAKERS";

    public static readonly string FirstPage = SearchUrl("camiseta%20dry%20fit", 0, "MLB-T_SHIRTS");

    public static readonly string SecondPage = SearchUrl("camiseta%20dry%20fit", 50, "MLB-T_SHIRTS");

    public static readonly string RegatasPage = SearchUrl("regata", 0, "MLB-TANK_TOPS");

    public const string TwinsCategory = "Tênis=tenis gemeos@MLB-SNEAKERS";

    public static readonly string SneakersPage = SearchUrl("tenis%20veloz", 0, "MLB-SNEAKERS");

    public static readonly string TwinsPage = SearchUrl("tenis%20gemeos", 0, "MLB-SNEAKERS");

    public static readonly IReadOnlyDictionary<string, SKColor> Markers = new Dictionary<string, SKColor>
    {
        ["700001-MLA2001"] = new(200, 0, 0),
        ["700002-MLA2001"] = new(0, 160, 0),
        ["700003-MLA2002"] = new(0, 0, 200),
        ["700005-MLA2004"] = new(200, 200, 0),
        ["700010-MLA2010"] = new(0, 200, 200),
        ["710002-MLA2021"] = new(200, 0, 200),
        ["720031-MLA2031"] = new(120, 60, 0),
        ["720032-MLA2032"] = new(0, 90, 160),
        ["720041-MLA2041"] = new(160, 160, 160),
        ["720042-MLA2042"] = new(60, 0, 120),
        ["720051-MLA2051"] = new(30, 30, 30),
        ["720052-MLA2052"] = new(230, 230, 230),
        ["730060-MLA2060"] = new(250, 200, 0),
        ["730061-MLA2061"] = new(100, 0, 160),
    };

    private static readonly string[] Products =
    [
        "MLB2000", "MLB2001", "MLB2002", "MLB2003", "MLB2004", "MLB2010", "MLB2020", "MLB2021",
        "MLB2030", "MLB2031", "MLB2032", "MLB2040", "MLB2041", "MLB2042",
        "MLB2050", "MLB2051", "MLB2052", "MLB2060", "MLB2061",
    ];

    public static string SearchUrl(string query, int offset, string domain) =>
        $"{Api}products/search?status=active&site_id=MLB&q={query}&limit=50&offset={offset}&domain_id={domain}";

    public static FixtureHandler Handler()
    {
        var handler = new FixtureHandler()
            .Json(FirstPage, "search-camisetas-0.json")
            .Json(SecondPage, "search-camisetas-50.json")
            .Json(RegatasPage, "search-regatas-0.json")
            .Json(SneakersPage, "search-tenis-0.json")
            .Json($"{Api}products/MLB2002/items", "items-MLB2002.json")
            .Json(TwinsPage, "search-gemeos-0.json")
            .Json($"{Api}products/MLB2003/items", "items-not-found.json", HttpStatusCode.NotFound)
            .Json($"{Api}products/MLB2099", "items-not-found.json", HttpStatusCode.NotFound)
            .Bytes($"{Picture}710001-MLA2020-F.jpg", Encoding.UTF8.GetBytes("<html>Erro 503</html>"));
        foreach (var id in Products)
        {
            handler.Json($"{Api}products/{id}", $"product-{id}.json");
        }

        foreach (var (picture, marker) in Markers)
        {
            var bytes = picture == "700001-MLA2001" ? TestImages.Jpeg(1200, 750, marker) : TestImages.Jpeg(600, 600, marker);
            handler.Bytes($"{Picture}{picture}-F.jpg", bytes);
        }

        return handler;
    }
}
