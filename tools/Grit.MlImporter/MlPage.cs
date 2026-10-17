namespace Grit.MlImporter;

// The "results" envelope of /products/search and /products/{id}/items; paging is not needed for the first page.
internal sealed record MlPage<T>(IReadOnlyList<T> Results);
