import { getFacets, getProducts } from "@/lib/api/catalog-reads";
import type { Suggestions } from "@/lib/search-suggestions";

const productLimit = 5;
const categoryLimit = 3;

// The browser cannot reach the API (its URL is server-only), so the search box asks for suggestions here.
// Products come from the search itself, so what the panel shows is what the listing would; categories come from its
// facets, so each one carries the number of products the typed text finds in it.
export async function GET(request: Request) {
  const q = new URL(request.url).searchParams.get("q")?.trim() ?? "";
  const empty: Suggestions = { products: [], categories: [] };
  if (q.length < 2) return Response.json(empty);

  const [products, facets] = await Promise.all([getProducts({ q, limit: productLimit }), getFacets({ q })]);
  const suggestions: Suggestions = {
    products: (products.data?.items ?? []).map(({ slug, name, priceAmount, hasPriceRange, imageUrl }) => ({ slug, name, priceAmount, hasPriceRange, imageUrl })),
    categories: (facets.data?.categories ?? []).filter((category) => category.count > 0).slice(0, categoryLimit),
  };
  return Response.json(suggestions);
}
