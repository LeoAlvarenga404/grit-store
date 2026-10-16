export type Filters = { q?: string; category?: string; minPrice?: string; maxPrice?: string; after?: string };

// Any filter change starts again from the first page, so `after` is dropped unless the change sets it.
export function catalogHref(filters: Filters, changes: Partial<Filters>): string {
  const params = new URLSearchParams();
  for (const [key, value] of Object.entries({ ...filters, after: undefined, ...changes })) {
    if (value) params.set(key, value);
  }
  const query = params.toString();
  return query ? `/?${query}` : "/";
}

export function toSearchQuery(filters: Filters) {
  return {
    q: filters.q || undefined,
    category: filters.category || undefined,
    minPrice: filters.minPrice ? Number(filters.minPrice) : undefined,
    maxPrice: filters.maxPrice ? Number(filters.maxPrice) : undefined,
  };
}
