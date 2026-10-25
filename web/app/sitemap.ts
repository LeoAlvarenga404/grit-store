import type { MetadataRoute } from "next";
import { getProducts } from "@/lib/api/catalog-reads";
import { siteUrl } from "@/lib/site-url";
import { storeCategories } from "@/lib/store-navigation";

// Read at request time: the web build has no API to list the products from.
export const dynamic = "force-dynamic";

// The search takes at most 50 products per page.
const pageSize = 50;
const maxPages = 40;

async function productSlugs(): Promise<string[]> {
  const slugs: string[] = [];
  let after: string | undefined;
  for (let page = 0; page < maxPages; page++) {
    const { data } = await getProducts({ limit: pageSize, after });
    if (!data) break;
    slugs.push(...data.items.map((item) => item.slug));
    if (!data.nextCursor) break;
    after = data.nextCursor;
  }
  return slugs;
}

export default async function sitemap(): Promise<MetadataRoute.Sitemap> {
  const pages = ["", "/catalog", ...storeCategories.map((category) => `/catalog?category=${category.slug}`)];
  const products = (await productSlugs()).map((slug) => `/products/${slug}`);
  return [...pages, ...products].map((path) => ({ url: `${siteUrl}${path}` }));
}
