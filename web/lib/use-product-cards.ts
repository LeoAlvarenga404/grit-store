import { useEffect, useState } from "react";
import type { CardProduct } from "./product-card-data";

// The browser keeps only slugs: prices and signed photo URLs are read again, so a card is never stale.
export function useProductCards(slugs: string[]): { products: CardProduct[]; isLoading: boolean } {
  const key = slugs.join(",");
  const [loaded, setLoaded] = useState<{ key: string; products: CardProduct[] }>({ key: "", products: [] });

  useEffect(() => {
    if (!key) return;
    const controller = new AbortController();
    fetch(`/api/products-by-slug?slugs=${encodeURIComponent(key)}`, { signal: controller.signal })
      .then((response) => (response.ok ? (response.json() as Promise<CardProduct[]>) : []))
      .then((products) => setLoaded({ key, products }))
      .catch(() => undefined);
    return () => controller.abort();
  }, [key]);

  return { products: loaded.key === key ? loaded.products : [], isLoading: key !== "" && loaded.key !== key };
}
