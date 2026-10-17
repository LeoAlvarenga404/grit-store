namespace Grit.MlImporter;

// The "results" envelope of /products/search and /products/{id}/items; Paging tells the search whether more pages exist.
internal sealed record MlPage<T>(IReadOnlyList<T> Results, MlPaging? Paging = null);
