import { unstable_cache } from "next/cache";
import { api } from "@/lib/api/client";
import type { SearchQuery } from "@/app/(store)/catalog/catalog-href";
import type { paths } from "./schema";

export const catalogTag = "catalog";

// The API signs every image URL for one hour (ADR 0016). The window is part of the cache key, so an entry from an older
// window is never read, however long the process stayed idle: no page is served with an URL that is about to expire.
const windowInSeconds = 600;

class Uncacheable extends Error {
  constructor(readonly status: number) {
    super(`The catalogue answered ${status}.`);
  }
}

// An answer without data (404, 500, a bad filter) is thrown out of the cache scope, so it is never stored.
async function read<T>(call: Promise<{ data?: T; response: Response }>): Promise<T> {
  const { data, response } = await call;
  if (!data) throw new Uncacheable(response.status);
  return data;
}

function cached<Args extends unknown[], T>(name: string, load: (...args: Args) => Promise<T>) {
  return async (...args: Args): Promise<{ data: T | null; status: number }> => {
    const window = String(Math.floor(Date.now() / (windowInSeconds * 1000)));
    try {
      const data = await unstable_cache(load, [name, window], { tags: [catalogTag], revalidate: windowInSeconds })(...args);
      return { data, status: 200 };
    } catch (error) {
      if (error instanceof Uncacheable) return { data: null, status: error.status };
      throw error;
    }
  };
}

type FacetQuery = NonNullable<paths["/api/v1/catalog/facets"]["get"]["parameters"]["query"]>;

export const getProducts = cached("products", (query: SearchQuery) => read(api.GET("/api/v1/catalog/products", { params: { query } })));

export const getFacets = cached("facets", (query: FacetQuery) => read(api.GET("/api/v1/catalog/facets", { params: { query } })));

export const getProductBySlug = cached("product", (slug: string) => read(api.GET("/api/v1/catalog/products/{slug}", { params: { path: { slug } } })));

export const getSuggestions = cached("suggestions", (q: string) => read(api.GET("/api/v1/catalog/suggestions", { params: { query: { q } } })));
