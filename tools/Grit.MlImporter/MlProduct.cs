namespace Grit.MlImporter;

// A catalog product. A family is a parent with children (one per variant); pickers link a variant to its siblings in
// another colour or size. FamilyName is the model's name without the variant.
internal sealed record MlProduct(
    string Id,
    string Name,
    IReadOnlyList<MlAttribute>? Attributes,
    IReadOnlyList<MlPicture>? Pictures,
    MlShortDescription? ShortDescription,
    string? FamilyName = null,
    string? ParentId = null,
    IReadOnlyList<string>? ChildrenIds = null,
    IReadOnlyList<MlPicker>? Pickers = null,
    MlBuyBoxWinner? BuyBoxWinner = null);
