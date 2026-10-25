"use client";

import { Heart } from "lucide-react";
import { EmptyState } from "@/components/empty-state";
import { ProductCard } from "@/components/product-card";
import { Skeleton } from "@/components/ui/skeleton";
import { favorites } from "@/lib/shopper-lists";
import { useSlugs } from "@/lib/slug-store";
import { useProductCards } from "@/lib/use-product-cards";

export function FavoritesList() {
  const slugs = useSlugs(favorites);
  const { products, isLoading } = useProductCards(slugs);

  if (slugs.length === 0) {
    return (
      <EmptyState
        icon={Heart}
        title="Nenhum favorito ainda"
        text="Toque no coração de um produto para guardá-lo aqui."
        actionLabel="Ver lançamentos"
        actionHref="/catalog"
      />
    );
  }

  if (isLoading) {
    return (
      <ul className="grid grid-cols-2 gap-x-3 gap-y-8 md:grid-cols-3 xl:grid-cols-4" aria-busy="true">
        {slugs.slice(0, 8).map((slug) => (
          <li key={slug}>
            <Skeleton className="aspect-[4/5] w-full" />
            <Skeleton className="mt-3 h-4 w-3/4" />
          </li>
        ))}
      </ul>
    );
  }

  return (
    <ul className="grid grid-cols-2 gap-x-3 gap-y-8 md:grid-cols-3 xl:grid-cols-4">
      {products.map((product) => (
        <li key={product.id}>
          <ProductCard product={product} />
        </li>
      ))}
    </ul>
  );
}
