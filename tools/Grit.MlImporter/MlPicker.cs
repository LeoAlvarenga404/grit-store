namespace Grit.MlImporter;

internal sealed record MlPicker(string PickerId, IReadOnlyList<MlPickerProduct>? Products);
