"use server";

import { api } from "@/lib/api/client";
import type { SearchQuery } from "./catalog-href";

// The browser cannot reach the API (its URL is server-only), so "Carregar mais" asks for the next page here.
export async function loadMoreProducts(query: SearchQuery, after: string) {
  const { data } = await api.GET("/api/v1/catalog/products", { params: { query: { ...query, after } } });
  return data ?? null;
}
