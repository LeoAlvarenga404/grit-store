"use client";

import { useState, useTransition } from "react";
import { ErrorMessage } from "@/components/error-message";
import { ProductCard } from "@/components/product-card";
import { Button } from "@/components/ui/button";
import type { components } from "@/lib/api/schema";
import { loadMoreProducts } from "./actions";
import type { SearchQuery } from "./catalog-href";

type Product = components["schemas"]["ProductSummaryResponse"];

// The page renders this with key = the current URL, so a new filter or sort starts again from the first page.
export function ProductGrid({ query, initialProducts, initialCursor }: { query: SearchQuery; initialProducts: Product[]; initialCursor: string | null }) {
  const [products, setProducts] = useState(initialProducts);
  const [cursor, setCursor] = useState(initialCursor);
  const [hasFailed, setHasFailed] = useState(false);
  const [isPending, startTransition] = useTransition();

  function loadMore() {
    if (!cursor) return;
    startTransition(async () => {
      const page = await loadMoreProducts(query, cursor);
      startTransition(() => {
        setHasFailed(!page);
        if (!page) return;
        setProducts((current) => [...current, ...page.items]);
        setCursor(page.nextCursor);
      });
    });
  }

  return (
    <>
      <ul className="grid grid-cols-2 gap-x-3 gap-y-8 lg:grid-cols-4">
        {products.map((product) => (
          <li key={product.id}>
            <ProductCard product={product} />
          </li>
        ))}
      </ul>
      <div className="mt-12 flex flex-col items-center gap-4" aria-live="polite">
        {hasFailed && <ErrorMessage>Não foi possível carregar mais produtos. Tente de novo.</ErrorMessage>}
        {!cursor && products.length > initialProducts.length && <p className="text-sm text-muted-foreground">Todos os produtos foram carregados.</p>}
        {cursor && (
          <Button variant="outline" size="lg" onClick={loadMore} disabled={isPending}>
            {isPending ? "Carregando…" : "Carregar mais"}
          </Button>
        )}
      </div>
    </>
  );
}
