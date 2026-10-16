namespace Grit.IntegrationTests;

[CollectionDefinition(Name)]
public sealed class SearchCorpusDefinition : ICollectionFixture<SearchCorpus>
{
    public const string Name = "SearchCorpus";
}
