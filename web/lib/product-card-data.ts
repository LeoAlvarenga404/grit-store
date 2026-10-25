import type { components } from "@/lib/api/schema";

type Product = components["schemas"]["GetProductBySlugResponse"];
export type CardProduct = components["schemas"]["ProductSummaryResponse"];

// The product page answer carries what a card needs; the cheapest SKU gives the "a partir de" price.
export function toCardProduct(product: Product): CardProduct {
  const prices = product.skus.map((sku) => sku.priceAmount);
  return {
    id: product.id,
    slug: product.slug,
    name: product.name,
    priceAmount: Math.min(...prices),
    priceCurrency: product.skus[0]?.priceCurrency ?? "BRL",
    hasPriceRange: new Set(prices).size > 1,
    imageUrl: product.imageUrls[0] ?? null,
    colors: product.colors.map((color) => ({ name: color.name, imageUrl: color.imageUrls[0] ?? null })),
  };
}
