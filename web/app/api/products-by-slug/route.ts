import { getProductBySlug } from "@/lib/api/catalog-reads";
import { toCardProduct } from "@/lib/product-card-data";

const maxSlugs = 48;
const slugPattern = /^[a-z0-9-]{1,120}$/;

// The browser cannot reach the API, and keeps only slugs (favourites, recently viewed): this turns them into cards.
// A product that no longer exists is left out.
export async function GET(request: Request) {
  const slugs = (new URL(request.url).searchParams.get("slugs") ?? "")
    .split(",")
    .filter((slug) => slugPattern.test(slug))
    .slice(0, maxSlugs);

  const results = await Promise.all(slugs.map((slug) => getProductBySlug(slug)));
  return Response.json(results.flatMap(({ data }) => (data ? [toCardProduct(data)] : [])));
}
