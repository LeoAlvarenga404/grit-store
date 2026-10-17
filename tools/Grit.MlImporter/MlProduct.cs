namespace Grit.MlImporter;

internal sealed record MlProduct(
    string Id,
    string Name,
    IReadOnlyList<MlAttribute>? Attributes,
    IReadOnlyList<MlPicture>? Pictures,
    MlShortDescription? ShortDescription);
