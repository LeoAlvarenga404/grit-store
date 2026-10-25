"use client";

import { ArrowDown, LoaderCircle } from "lucide-react";
import { useState, useTransition } from "react";
import { ErrorMessage } from "@/components/error-message";
import { ProductCard } from "@/components/product-card";
import { Button } from "@/components/ui/button";
import { Skeleton } from "@/components/ui/skeleton";
import type { components } from "@/lib/api/schema";
import { loadMoreProducts } from "./actions";
import type { SearchQuery } from "./catalog-href";

type Product = components["schemas"]["ProductSummaryResponse"];

const skeletonCount = 4;

// The page renders this with key = the current URL, so a new filter or sort starts again from the first page.
export function ProductGrid({
  query,
  initialProducts,
  initialCursor,
  totalProducts,
}: {
  query: SearchQuery;
  initialProducts: Product[];
  initialCursor: string | null;
  totalProducts: number | null;
}) {
  const [products, setProducts] = useState(initialProducts);
  const [cursor, setCursor] = useState(initialCursor);
  const [hasFailed, setHasFailed] = useState(false);
  const [isPending, startTransition] = useTransition();
  const total = Math.max(totalProducts ?? products.length, products.length);

  function loadMore() {
    if (!cursor) return;
    startTransition(async () => {
      // A network failure rejects the Server Function; caught here it shows the retry message instead of the error page, and the cursor stays for the retry.
      const page = await loadMoreProducts(query, cursor).catch(() => null);
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
      <ul className="grid grid-cols-2 gap-x-3 gap-y-10 lg:grid-cols-4">
        {products.map((product) => (
          <li key={product.id} className="reveal">
            <ProductCard product={product} />
          </li>
        ))}
        {isPending &&
          Array.from({ length: skeletonCount }, (_, index) => (
            <li key={`loading-${index}`} aria-hidden="true">
              <Skeleton className="aspect-[4/5] w-full" />
              <Skeleton className="mt-3 h-4 w-3/4" />
              <Skeleton className="mt-2 h-4 w-1/3" />
            </li>
          ))}
      </ul>
      <div className="mt-16 flex flex-col items-center gap-5 border-t pt-10" aria-live="polite">
        {hasFailed && <ErrorMessage>Não foi possível carregar mais produtos. Tente de novo.</ErrorMessage>}
        <p className="text-sm text-muted-foreground">
          Você viu <span className="font-semibold text-foreground tabular-nums">{products.length}</span> de <span className="font-semibold text-foreground tabular-nums">{total}</span> produtos
        </p>
        <div
          role="progressbar"
          aria-label="Produtos exibidos"
          aria-valuemin={0}
          aria-valuemax={total}
          aria-valuenow={products.length}
          className="h-1 w-64 max-w-full overflow-hidden rounded-full bg-border"
        >
          <div className="h-full rounded-full bg-foreground transition-[width] duration-700 ease-out" style={{ width: `${(products.length / total) * 100}%` }} />
        </div>
        {cursor && (
          <Button variant="outline" size="lg" onClick={loadMore} disabled={isPending} className="min-w-64 group">
            {isPending ? (
              <>
                <LoaderCircle aria-hidden="true" className="animate-spin" /> Carregando
              </>
            ) : (
              <>
                Ver mais produtos <ArrowDown aria-hidden="true" className="transition-transform group-hover:translate-y-0.5" />
              </>
            )}
          </Button>
        )}
      </div>
    </>
  );
}
