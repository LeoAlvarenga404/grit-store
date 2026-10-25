"use server";

import { getProducts } from "@/lib/api/catalog-reads";
import type { SearchQuery } from "./catalog-href";

// The browser cannot reach the API (its URL is server-only), so "Carregar mais" asks for the next page here.
export async function loadMoreProducts(query: SearchQuery, after: string) {
  const { data } = await getProducts({ ...query, after });
  return data;
}
