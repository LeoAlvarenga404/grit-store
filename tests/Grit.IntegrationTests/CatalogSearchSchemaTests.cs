using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace Grit.IntegrationTests;

public class CatalogSearchSchemaTests(ApiFactory factory)
{
    private readonly NpgsqlDataSource _dataSource = factory.Services.GetRequiredService<NpgsqlDataSource>();

    [Fact]
    public async Task SearchConfiguration_AccentsAndPlurals_BecomeTheSameLexemes()
    {
        var document = await ScalarAsync<string>("SELECT to_tsvector('catalog.portuguese_unaccent', 'Tênis Camisetas Térmicas Café')::text");
        var query = await ScalarAsync<string>("SELECT plainto_tsquery('catalog.portuguese_unaccent', 'tenis camiseta termica cafe')::text");

        document.ShouldBe("'caf':4 'camiset':2 'ten':1 'termic':3");
        query.ShouldBe("'ten' & 'camiset' & 'termic' & 'caf'");
    }

    // Hyphenated words go through the same unaccent + stem chain as plain words, whole and in parts.
    [Fact]
    public async Task SearchConfiguration_HyphenatedAccentedWord_IndexesTheWholeAndItsParts()
    {
        var document = await ScalarAsync<string>("SELECT to_tsvector('catalog.portuguese_unaccent', 'Pós-Treino')::text");

        document.ShouldBe("'pos':2 'pos-trein':1 'trein':3");
    }

    [Fact]
    public async Task ImmutableUnaccent_AccentedText_ReturnsPlainLetters()
    {
        var text = await ScalarAsync<string>("SELECT catalog.immutable_unaccent('Térmica Café Ção')");

        text.ShouldBe("Termica Cafe Cao");
    }

    // The product name ends in a random hex token that may split into several lexemes, so only the description's weights are pinned.
    [Fact]
    public async Task SearchVector_ProductCreatedThenRenamed_FollowsNameAndDescription()
    {
        var (id, _) = await CatalogAdminApi.CreateProductAsync(factory.CreateClient());

        var created = await ScalarAsync<string>($"SELECT search_vector::text FROM catalog.products WHERE id = '{id}'");
        await ScalarAsync<int>($"UPDATE catalog.products SET name = 'Mochila Leve' WHERE id = '{id}' RETURNING 1");
        var renamed = await ScalarAsync<string>($"SELECT search_vector::text FROM catalog.products WHERE id = '{id}'");

        created.ShouldContain("'camiset':1A");
        created.ShouldMatch(@"'dry':\d+C");
        created.ShouldMatch(@"'fit':\d+C");
        renamed.ShouldBe("'dry':3C 'fit':4C 'lev':2A 'mochil':1A");
    }

    [Theory]
    [InlineData("ix_products_search_vector", "CREATE INDEX ix_products_search_vector ON catalog.products USING gin (search_vector)")]
    [InlineData("ix_products_name_trigram", "CREATE INDEX ix_products_name_trigram ON catalog.products USING gin (catalog.immutable_unaccent((name)::text) gin_trgm_ops)")]
    public async Task SearchIndex_AfterMigration_IsAGinIndex(string name, string definition)
    {
        var actual = await ScalarAsync<string>($"SELECT indexdef FROM pg_indexes WHERE schemaname = 'catalog' AND indexname = '{name}'");

        actual.ShouldBe(definition);
    }

    // The search relies on pg_trgm's default strict word similarity threshold (0.5): one wrong letter matches, another word does not.
    [Theory]
    [InlineData("camizeta", true)]
    [InlineData("camisa", false)]
    public async Task StrictWordSimilarity_DefaultThreshold_ToleratesOneWrongLetter(string text, bool isMatch)
    {
        var actual = await ScalarAsync<bool>($"SELECT '{text}' <<% catalog.immutable_unaccent('Camiseta Dry Fit')");

        actual.ShouldBe(isMatch);
    }

    private async Task<T> ScalarAsync<T>(string sql)
    {
        await using var command = _dataSource.CreateCommand(sql);
        return (T)(await command.ExecuteScalarAsync(TestContext.Current.CancellationToken))!;
    }
}
