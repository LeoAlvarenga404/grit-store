import type { components } from "@/lib/api/schema";

export type SuggestedProduct = Pick<components["schemas"]["ProductSummaryResponse"], "slug" | "name" | "priceAmount" | "hasPriceRange" | "imageUrl">;
export type SuggestedCategory = Pick<components["schemas"]["CategoryFacetResponse"], "slug" | "name" | "count">;
export type Suggestions = { products: SuggestedProduct[]; categories: SuggestedCategory[] };

export type SuggestionEntry = { key: string; href: string };

// Categories first, then products, then the full listing: the order the panel renders and the arrow keys walk.
export function suggestionEntries(text: string, suggestions: Suggestions): SuggestionEntry[] {
  const q = encodeURIComponent(text);
  return [
    ...suggestions.categories.map((category) => ({ key: `category-${category.slug}`, href: `/catalog?category=${category.slug}&q=${q}` })),
    ...suggestions.products.map((product) => ({ key: `product-${product.slug}`, href: `/products/${product.slug}` })),
    { key: "all", href: `/catalog?q=${q}` },
  ];
}
