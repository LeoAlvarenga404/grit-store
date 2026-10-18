import type { components } from "@/lib/api/schema";
import { compareSizes } from "./sizes.ts";

type Sku = components["schemas"]["GetProductBySlugSkuResponse"];
type Color = components["schemas"]["GetProductBySlugColorResponse"];

export type SizeOption = { label: string; sku: Sku };

const colorAttribute = "cor";
const sizeAttribute = "tamanho";
const singleOptionLabel = "Único";

// Seller bookkeeping that means nothing to a shopper.
const hiddenDetails = new Set(["id-da-guia-de-tamanhos", "id-da-linha-da-guia-de-tamanhos", "modelo-alfanumerico", "mpn"]);

// ?cor= picks the colour; a missing or unknown value falls back to the first colour (the API lists colours with photos first).
export function selectColor(colors: Color[], requested: string | undefined): Color | null {
  return colors.find((color) => color.name === requested) ?? colors[0] ?? null;
}

export function skusOfColor(skus: Sku[], color: Color | null): Sku[] {
  return color ? skus.filter((sku) => sku.attributes[colorAttribute] === color.name) : skus;
}

function optionLabel(sku: Sku): string {
  const size = sku.attributes[sizeAttribute];
  if (size) return size;
  const others = Object.entries(sku.attributes).filter(([key]) => key !== colorAttribute);
  return others.map(([, value]) => value).join(" · ") || singleOptionLabel;
}

// Only the sizes that exist as SKUs of the colour, from small to large.
export function sizeOptions(skus: Sku[]): SizeOption[] {
  return skus.map((sku) => ({ label: optionLabel(sku), sku })).sort((left, right) => compareSizes(left.label, right.label));
}

export function isSingleOption(options: SizeOption[]): boolean {
  return options.length === 1 && options[0].label === singleOptionLabel;
}

export function lowestPrice(skus: Sku[]): { amount: number; hasRange: boolean } | null {
  if (skus.length === 0) return null;
  const amounts = skus.map((sku) => sku.priceAmount);
  const amount = Math.min(...amounts);
  return { amount, hasRange: amounts.some((value) => value !== amount) };
}

export function galleryOf(productImageUrls: string[], color: Color | null): string[] {
  return color && color.imageUrls.length > 0 ? color.imageUrls : productImageUrls;
}

// "materiais-da-sola" → "Materiais da sola".
export function productDetails(attributes: Record<string, string>): { label: string; value: string }[] {
  return Object.entries(attributes)
    .filter(([key]) => !hiddenDetails.has(key))
    .map(([key, value]) => ({ label: key.charAt(0).toUpperCase() + key.slice(1).replaceAll("-", " "), value }));
}
