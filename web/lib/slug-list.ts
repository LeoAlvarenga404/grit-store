// Pure list helpers for the shopper's own lists (favourites, recently viewed), kept in the browser.
export function parseSlugs(raw: string | null): string[] {
  try {
    const value: unknown = JSON.parse(raw ?? "[]");
    return Array.isArray(value) ? value.filter((item): item is string => typeof item === "string") : [];
  } catch {
    return [];
  }
}

// Newest first, no duplicates, at most `max`.
export function addSlug(list: string[], slug: string, max: number): string[] {
  return [slug, ...list.filter((item) => item !== slug)].slice(0, max);
}

export function toggleSlug(list: string[], slug: string, max: number): string[] {
  return list.includes(slug) ? list.filter((item) => item !== slug) : addSlug(list, slug, max);
}
